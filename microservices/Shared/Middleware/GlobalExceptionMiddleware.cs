using System.Net;
using System.Text.Json;
using KBA.Framework.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KBA.Microservices.Shared.Middleware;

/// <summary>
/// Middleware global pour gérer toutes les exceptions de manière standardisée
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();

        _logger.LogError(
            exception,
            "Une erreur s'est produite. CorrelationId: {CorrelationId}",
            correlationId
        );

        context.Response.ContentType = "application/json";
        
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Ressource non trouvée"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Non autorisé"),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Une erreur interne s'est produite")
        };

        context.Response.StatusCode = (int)statusCode;

        var errorDetails = new ErrorDetails
        {
            StatusCode = (int)statusCode,
            Message = message,
            CorrelationId = correlationId,
            // Stack trace uniquement en développement
            StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}

/// <summary>
/// Extension pour faciliter l'utilisation du middleware
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
