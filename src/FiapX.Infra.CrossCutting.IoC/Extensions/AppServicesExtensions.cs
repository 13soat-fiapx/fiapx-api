using FiapX.Application.ProcessingJobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting.IoC.Extensions;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<ProcessingJobAppService>();

        return services;
    }
}
