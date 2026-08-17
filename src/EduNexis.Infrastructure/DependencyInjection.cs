using EduNexis.Application.Abstractions;
using EduNexis.Domain.Interfaces.Repositories;
using EduNexis.Infrastructure.Persistence;
using EduNexis.Infrastructure.Persistence.Repositories;
using EduNexis.Infrastructure.Services.Cache;
using EduNexis.Infrastructure.Services.Email;
using EduNexis.Infrastructure.Services.Sms;
using EduNexis.Infrastructure.Services;
using EduNexis.Infrastructure.Services.Auth;
using EduNexis.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace EduNexis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + MySQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection")!,
                new MySqlServerVersion(new Version(8, 0, 36)),
                mySql => mySql
                    .EnableRetryOnFailure(3)
                    .CommandTimeout(30)));

        // Repositories + UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Current User (HttpContext)
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Auth services (JWT + password hashing)
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IOtpGenerator, OtpGenerator>();
        services.AddSingleton<IResetTokenGenerator, ResetTokenGenerator>();
        services.AddSingleton<IAuthSettings, AuthSettings>();


        // Cloudinary Storage
        services.AddScoped<IFileStorageService, CloudinaryStorageService>();

        // Email via Brevo HTTP API (SMTP blocked on Render free tier)
        services.AddHttpClient("brevo-email");
        services.AddScoped<IEmailService, EmailService>();

        services.AddHttpClient("sms-gateway");
        services.AddScoped<ISmsService, SmsService>();
        services.AddSingleton<IEmailTemplateBuilder, EmailTemplateBuilder>();

        // Redis Cache (optional, falls back to in-memory if no Redis URL)
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn + ",abortConnect=false,connectTimeout=3000,syncTimeout=3000";
                options.InstanceName = "EduNexis:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}
