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

        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId =
                context.Request.Headers.TryGetValue(HeaderName, out var incoming)
                && !string.IsNullOrWhiteSpace(incoming)
                    ? incoming.ToString()
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
    }
}
