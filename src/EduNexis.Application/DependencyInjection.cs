using Microsoft.Extensions.DependencyInjection;

namespace EduNexis.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application-layer services here, for example:
        // services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
