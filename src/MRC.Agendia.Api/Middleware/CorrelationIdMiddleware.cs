using Serilog.Context;

namespace MRC.Agendia.Api.Middleware
{
    /// <summary>
    /// Assigns a correlation id to every request so a reported error can be
    /// traced through the logs:
    ///   - Reads the incoming <c>X-Correlation-Id</c> header, or generates one.
    ///   - Sets it as <see cref="HttpContext.TraceIdentifier"/> so the traceId
    ///     returned by <c>ExceptionHandlingMiddleware</c> is the same value.
    ///   - Pushes it into Serilog's <see cref="LogContext"/> (the logger already
    ///     enriches FromLogContext) so every log line for the request carries it.
    ///   - Echoes it back on the response header.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-Id";

        // Cap the adopted id so a client cannot bloat every log line, and only accept a
        // safe charset so a forged header cannot inject CRLF/control chars into the logs.
        private const int MaxLength = 64;

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                ? Sanitize(incoming.ToString())
                : Guid.NewGuid().ToString();

            context.TraceIdentifier = correlationId;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }

        private static string Sanitize(string? incoming)
        {
            if (string.IsNullOrWhiteSpace(incoming))
                return Guid.NewGuid().ToString();

            var trimmed = incoming.Trim();
            return trimmed.Length <= MaxLength && IsSafe(trimmed)
                ? trimmed
                : Guid.NewGuid().ToString();
        }

        private static bool IsSafe(string value)
        {
            foreach (var c in value)
                if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                    return false;
            return true;
        }
    }
}
