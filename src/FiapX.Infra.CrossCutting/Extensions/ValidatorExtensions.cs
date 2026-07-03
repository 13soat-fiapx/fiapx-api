using FiapX.Application.Utils;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FiapX.Infra.CrossCutting;

public static class ValidatorExtensions
{
    public static IServiceCollection AddRequestValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(IAppService).Assembly);

        return services;
    }
}
