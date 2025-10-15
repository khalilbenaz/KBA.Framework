using System.Net;
using System.Text.Json;

namespace KBA.Framework.Api.Middleware;

/// <summary>
/// Middleware pour la gestion globale des exceptions
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IWebHostEnvironment environment)
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
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "Une erreur inattendue s'est produite.";
        var details = string.Empty;

        switch (exception)
        {
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
                _logger.LogWarning(exception, "Ressource non trouvée: {Message}", exception.Message);
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Accès non autorisé.";
                _logger.LogWarning(exception, "Tentative d'accès non autorisé: {Message}", exception.Message);
                break;

            case ArgumentException:
            case InvalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
                _logger.LogWarning(exception, "Requête invalide: {Message}", exception.Message);
                break;

            default:
                _logger.LogError(exception, "Erreur non gérée: {Message}", exception.Message);
                break;
        }

        // En développement, inclure les détails de l'exception
        if (_environment.IsDevelopment())
        {
            details = exception.ToString();
        }

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Modèle de réponse d'erreur standardisé
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
