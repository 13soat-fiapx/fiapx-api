using FiapX.Api.Extensions;
using FiapX.Api.Middlewares;
using FiapX.Infra.CrossCutting;
using FiapX.Infra.Observability;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability(ObservabilityProfile.Api);

builder.Services
    .AddControllers(options =>
    {
        options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseParameterTransformer()));
        options.Filters.Add<RequestValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation(builder.Configuration);

builder.Services
    .AddJwtAuthentication(builder.Configuration)
    .AddDataRepositories(builder.Configuration)
    .AddStorage(builder.Configuration)
    .AddMessaging(builder.Configuration)
    .AddAppServices()
    .AddRequestValidators();

builder.Services.AddHealthChecks();
builder.Services.AddGlobalCorsPolicy();

var app = builder.Build();
var routePrefix = app.Configuration["AppInfo:RoutePrefix"];
if (!string.IsNullOrWhiteSpace(routePrefix))
    app.UsePathBase($"/{routePrefix.Trim('/')}");

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<DomainValidationMiddleware>();

app.UseRouting();
app.UseCors("AllowAllOrigins");
app.UseAuthentication();
app.UseAuthorization();

var controllers = app.MapControllers();
if (app.Configuration.IsAuthenticationEnabled())
    controllers.RequireAuthorization();

if (!app.Environment.IsProduction())
    app.UseSwaggerDocumentation(routePrefix);

app.UseHealthChecks("/health");

app.Run();

public partial class Program
{
}
