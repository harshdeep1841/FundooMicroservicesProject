using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Fundoo.Observability;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    internal const string ItemKey = "Fundoo.CorrelationId";
    private const int MaximumLength = 128;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Read incoming correlation ID
        var incomingId = context.Request.Headers[HeaderName].ToString();

        // 2. Log the incoming ID for debugging
        _logger.LogWarning(
            "DEBUG: Incoming correlation header = {IncomingId}",
            incomingId);

        // 3. Validate incoming ID
        var correlationId = incomingId;

        if (!IsValid(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");

            _logger.LogInformation(
                "No valid incoming correlation ID. Generated new ID: {CorrelationId}",
                correlationId);
        }
        else
        {
            _logger.LogInformation(
                "Reusing incoming correlation ID: {CorrelationId}",
                correlationId);
        }

        // 4. Store ID in current request context
        context.Items[ItemKey] = correlationId;

        // 5. Return ID in response header
        context.Response.Headers[HeaderName] = correlationId;

        // 6. Add ID to Serilog LogContext
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > MaximumLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                continue;
            }

            if (character == '-' ||
                character == '_' ||
                character == '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
