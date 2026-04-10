using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace EscrowApp.Infrastructure.Middleware;

/// <summary>
/// Catches unhandled exceptions on /api routes and returns RFC 7807 ProblemDetails.
/// Non-API routes (Blazor) fall through to the standard error handler.
/// </summary>
public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business rule violation on {Path}", context.Request.Path);
            await WriteProblemDetails(context, HttpStatusCode.UnprocessableEntity,
                "Business Rule Violation", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Resource not found on {Path}", context.Request.Path);
            await WriteProblemDetails(context, HttpStatusCode.NotFound,
                "Resource Not Found", ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access on {Path}", context.Request.Path);
            await WriteProblemDetails(context, HttpStatusCode.Forbidden,
                "Forbidden", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteProblemDetails(context, HttpStatusCode.InternalServerError,
                "Internal Server Error", "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemDetails(
        HttpContext context, HttpStatusCode statusCode, string title, string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{(int)statusCode}"
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
