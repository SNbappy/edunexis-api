using EduNexis.Infrastructure.Persistence;
using EduNexis.Infrastructure.Persistence.Repositories;
using EduNexis.Infrastructure.Services.Auth;
using EduNexis.Infrastructure.Services.Cache;
using EduNexis.Infrastructure.Services.Email;
using EduNexis.Infrastructure.Services.Storage;
using FluentEmail.Core;
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
                ServerVersion.AutoDetect(
                    configuration.GetConnectionString("DefaultConnection")!),
                mySql => mySql
                    .EnableRetryOnFailure(3)
                    .CommandTimeout(30)));

        // Repositories + UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Firebase Auth
        services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();

        // Cloudinary Storage
        services.AddScoped<IFileStorageService, CloudinaryStorageService>();

        // Email via FluentEmail
        var emailConfig = configuration.GetSection("Email");
        services.AddFluentEmail(emailConfig["From"]!, emailConfig["SenderName"]!)
            .AddSmtpSender(
                emailConfig["Host"]!,
                int.Parse(emailConfig["Port"]!),
                emailConfig["Username"],
                emailConfig["Password"]);
        services.AddScoped<IEmailService, EmailService>();

        // Redis Cache
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis")!);
        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}
