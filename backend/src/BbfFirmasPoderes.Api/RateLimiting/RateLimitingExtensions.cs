using System.Threading.RateLimiting;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BbfFirmasPoderes.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddAuthorityRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();
        if (options.PermitLimit <= 0)
            options.PermitLimit = 200;
        if (options.WindowSeconds <= 0)
            options.WindowSeconds = 60;

        services.AddSingleton(options);
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, token) =>
            {
                var window = options.WindowSeconds;
                context.HttpContext.Response.Headers.RetryAfter = window.ToString();
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Type = "https://tools.ietf.org/html/rfc6585#section-4",
                    Detail = "Rate limit do consumer excedido.",
                    Instance = context.HttpContext.Request.Path
                };
                problem.Extensions["correlationId"] = CorrelationContext.Current
                    ?? context.HttpContext.Items[CorrelationIds.HeaderName]?.ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    problem,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: token);
            };

            limiter.AddPolicy(RateLimitingOptions.PolicyName, httpContext =>
            {
                var partition = httpContext.User.Identity?.Name ?? "anonymous";
                return RateLimitPartition.GetFixedWindowLimiter(
                    partition,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }
}
