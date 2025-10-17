using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace KBA.Microservices.Shared.Middleware;

/// <summary>
/// Middleware pour gérer les Correlation IDs à travers les services
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Ajouter au header de réponse
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Ajouter au contexte de logging
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Rendre disponible dans le HttpContext
            context.Items["CorrelationId"] = correlationId;

            await _next(context);
        }
    }
}

/// <summary>
/// Extension pour faciliter l'utilisation du middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
