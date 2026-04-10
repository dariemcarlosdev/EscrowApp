using System.Diagnostics;
using MediatR;

namespace EscrowApp.Features.Behaviors;

/// <summary>
/// Monitors MediatR request execution time. Logs a warning when a request
/// exceeds the configured threshold (default 500ms) to help identify
/// performance bottlenecks in production.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > WarningThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogWarning(
                "Long-running request: {RequestName} took {ElapsedMs}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
