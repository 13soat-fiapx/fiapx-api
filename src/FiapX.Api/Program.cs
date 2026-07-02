using FiapX.Api.Extensions;
using FiapX.Api.Middlewares;
using FiapX.Api.Security;
using FiapX.Infra.CrossCutting.IoC.Extensions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddApiAuthentication(builder.Configuration);

builder.Services
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

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<DomainValidationMiddleware>();

app.UseRouting();
app.UseCors("AllowAllOrigins");
app.UseApiAuthentication();

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
