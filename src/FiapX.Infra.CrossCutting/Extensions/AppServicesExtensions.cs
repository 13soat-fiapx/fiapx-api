using FiapX.Application.Auth.Services;
using FiapX.Application.ProcessingJobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<AuthAppService>();
        services.AddScoped<ProcessingJobAppService>();

        return services;
    }
}
