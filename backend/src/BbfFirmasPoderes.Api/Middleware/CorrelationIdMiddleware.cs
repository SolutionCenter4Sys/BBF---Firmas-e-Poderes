using BbfFirmasPoderes.Domain.Correlation;
using Serilog.Context;

namespace BbfFirmasPoderes.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIds.HeaderName].FirstOrDefault();
        var correlationId = CorrelationIds.FromHeaderOrNew(incoming);

        CorrelationContext.Set(correlationId);
        context.Items[CorrelationIds.HeaderName] = correlationId;
        context.Response.Headers[CorrelationIds.HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
