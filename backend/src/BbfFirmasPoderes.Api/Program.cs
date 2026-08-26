using BbfFirmasPoderes.Api.Auth;
using BbfFirmasPoderes.Api.Documents;
using BbfFirmasPoderes.Api.Logging;
using BbfFirmasPoderes.Api.Middleware;
using BbfFirmasPoderes.Api.OpenApi;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new PiiMaskingTextFormatter())
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, configuration) =>
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(new PiiMaskingTextFormatter()),
        preserveStaticLogger: true);

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddBbfAuth(builder.Configuration);
    builder.Services.AddBbfSwagger();
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["correlationId"] =
                CorrelationContext.Current
                ?? context.HttpContext.Items[CorrelationIds.HeaderName]?.ToString();
        };
    });

    var maxUploadBytes = builder.Configuration.GetValue("Documents:MaxUploadBytes", UploadRules.DefaultMaxBytes);
    if (maxUploadBytes <= 0)
        maxUploadBytes = UploadRules.DefaultMaxBytes;
    var requestCeiling = maxUploadBytes + (1024 * 1024);

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = requestCeiling;
        options.ValueLengthLimit = int.MaxValue;
        options.MultipartHeadersLengthLimit = 16 * 1024;
    });
    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = requestCeiling;
    });
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = requestCeiling;
    });

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseCorrelationId();
    app.UseSerilogRequestLogging();
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException ex) when (
            ex.StatusCode == StatusCodes.Status413PayloadTooLarge
            || ex.Message.Contains("body too large", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Multipart body length limit", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Unprocessable Entity",
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Detail = "Arquivo excede o limite de tamanho.",
                Instance = context.Request.Path
            };
            problem.Extensions["correlationId"] = CorrelationContext.Current
                ?? context.Items[CorrelationIds.HeaderName]?.ToString();
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    });
    app.UseBbfSwagger();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => false
    }).RequireAuthorization(BbfFirmasPoderes.Domain.Auth.Policies.HealthRead);

    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapDocuments();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API encerrada inesperadamente");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
