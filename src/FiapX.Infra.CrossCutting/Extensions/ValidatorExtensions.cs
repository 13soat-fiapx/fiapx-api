using FiapX.Application.ProcessingJobs.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class ValidatorExtensions
{
    public static IServiceCollection AddRequestValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(CreateProcessingJobRequestValidator).Assembly);

        return services;
    }
}
