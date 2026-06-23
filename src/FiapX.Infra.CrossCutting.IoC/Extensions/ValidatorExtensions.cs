using FiapX.Application.ProcessingJobs.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting.IoC.Extensions;

public static class ValidatorExtensions
{
    public static IServiceCollection AddRequestValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProcessingJobRequestValidator>();

        return services;
    }
}
