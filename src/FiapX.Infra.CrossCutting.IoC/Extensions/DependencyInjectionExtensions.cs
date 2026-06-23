using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting.IoC.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddFiapXServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDataRepositories(configuration)
            .AddStorage(configuration)
            .AddMessaging(configuration)
            .AddAppServices();

        return services;
    }
}
