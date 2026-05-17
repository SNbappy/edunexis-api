using EduNexis.API.Middleware;
using EduNexis.Application;
using EduNexis.Infrastructure;
using EduNexis.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

Directory.CreateDirectory("logs");

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "5041";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

// builder.Services.AddCors(options =>
//     options.AddPolicy("AllowFrontend", policy =>
//         policy.WithOrigins(
//             builder.Configuration.GetSection("Cors:AllowedOrigins")
//                 .Get<string[]>() ?? [])
//         .AllowAnyHeader()
//         .AllowAnyMethod()
//         .AllowCredentials()));

// added for netlify down code
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// added for netlify upper code

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddMediator(options =>
    options.ServiceLifetime = ServiceLifetime.Scoped);

var jwtSecret   = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer   = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Enums serialize as strings (e.g. "Text" not 0)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Force all DateTime values to serialize as ISO-8601 UTC ("...Z")
        // so client-side Date parsers never misinterpret them as local time.
        options.JsonSerializerOptions.Converters.Add(new EduNexis.API.Serialization.Iso8601UtcDateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new EduNexis.API.Serialization.Iso8601UtcNullableDateTimeConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduNexis API",
        Version = "v1",
        Description = "EduNexis Learning Management System API"
    });

    // Display enum names instead of integers
    c.UseInlineDefinitionsForEnums();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddHttpClient();

var app = builder.Build();

// ─────────────────────────────────────────────
// Apply pending EF Core migrations on startup
// so schema changes ship with code deploys.
// ─────────────────────────────────────────────
// Skip auto-migration in production by default. Schema is managed manually
// against Clever Cloud free MySQL (5-connection cap) — auto-migration on every
// container restart competes with the running instance for connections and
// crashes the new deploy. Enable explicitly via Database:RunMigrationsOnStartup=true
// when you genuinely need to apply pending migrations.
var runMigrations = builder.Configuration.GetValue<bool>("Database:RunMigrationsOnStartup", false);
if (runMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            Log.Information("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration failed on startup.");
            throw;
        }
    }
}
else
{
    Log.Information("Skipping startup migrations (Database:RunMigrationsOnStartup=false).");
}

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduNexis API v1");
    c.RoutePrefix = "swagger";
});

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", timestamp = DateTime.UtcNow }));

app.Run();

