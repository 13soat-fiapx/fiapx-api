using FiapX.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FiapX.Api.Security;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        if (!configuration.GetValue<bool>("Authentication:Enabled"))
            return services;

        services.AddAuthenticationWithoutValidation();
        services.AddAuthorization();

        return services;
    }

    public static IApplicationBuilder UseApiAuthentication(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Authentication:Enabled"))
            return app;

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static bool IsAuthenticationEnabled(this IConfiguration configuration) =>
        configuration.GetValue<bool>("Authentication:Enabled");

    private static IServiceCollection AddAuthenticationWithoutValidation(this IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    SignatureValidator = (token, _) => new JsonWebToken(token)
                };
            });

        return services;
    }
}
