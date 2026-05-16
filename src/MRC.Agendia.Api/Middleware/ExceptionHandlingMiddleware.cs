using System.Net;
using System.Text.Json;

namespace MRC.Agendia.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, code, message) = ex switch
            {
                KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN", ex.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, "BAD_REQUEST", ex.Message),
                ArgumentException => (HttpStatusCode.BadRequest, "BAD_REQUEST", ex.Message),
                _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Ha ocurrido un error inesperado.")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception");
            else
                _logger.LogWarning(ex, "Handled exception: {Code}", code);

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                code,
                message,
                traceId = context.TraceIdentifier
            }, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
