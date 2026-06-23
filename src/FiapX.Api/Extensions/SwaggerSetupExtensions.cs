using Microsoft.OpenApi.Models;
using System.Reflection;

namespace FiapX.Api.Extensions;

public static class SwaggerSetupExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = configuration["AppInfo:Name"] ?? "FIAP X - Media Processing API",
                Version = "v1",
                Description = configuration["AppInfo:Description"]
            });

            options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Bearer token."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    },
                    []
                }
            });

            IncludeXmlCommentsIfExists(options, Assembly.GetExecutingAssembly());
            IncludeXmlCommentsIfExists(options, typeof(FiapX.Application.ProcessingJobs.Services.ProcessingJobAppService).Assembly);
        });

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FIAP X API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }

    private static void IncludeXmlCommentsIfExists(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options, Assembly assembly)
    {
        var xmlFile = $"{assembly.GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
}
