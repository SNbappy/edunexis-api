using EduNexis.Infrastructure.Persistence;
using EduNexis.Infrastructure.Persistence.Repositories;
using EduNexis.Infrastructure.Services.Auth;
using EduNexis.Infrastructure.Services.Cache;
using EduNexis.Infrastructure.Services.Email;
using EduNexis.Infrastructure.Services.Storage;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace EduNexis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ─── EF Core + MySQL ─────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection")!,
                new MySqlServerVersion(new Version(8, 0, 36)),
                mySql => mySql
                    .EnableRetryOnFailure(3)
                    .CommandTimeout(30)));

        // ─── Repositories + UnitOfWork ───────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ─── Firebase Auth (lazy — no init at startup) ───────────────────────
        services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();

        // ─── Cloudinary Storage ──────────────────────────────────────────────
        services.AddScoped<IFileStorageService, CloudinaryStorageService>();

        // ─── Email via FluentEmail ───────────────────────────────────────────
        var emailConfig = configuration.GetSection("Email");
        services.AddFluentEmail(
                emailConfig["From"] ?? "noreply@edunexis.com",
                emailConfig["SenderName"] ?? "EduNexis")
            .AddSmtpSender(
                emailConfig["Host"] ?? "smtp.gmail.com",
                int.Parse(emailConfig["Port"] ?? "587"),
                emailConfig["Username"],
                emailConfig["Password"]);
        services.AddScoped<IEmailService, EmailService>();

        // ─── Redis Cache (abortConnect=false → non-blocking startup) ─────────
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration =
                configuration.GetConnectionString("Redis") + ",abortConnect=false,connectTimeout=3000,syncTimeout=3000";
            options.InstanceName = "EduNexis:";
        });
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}
