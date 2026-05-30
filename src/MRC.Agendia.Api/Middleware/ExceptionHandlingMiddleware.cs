using System.Net;
using System.Text.Json;
using FluentValidation;
using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Api.Middleware
{
    /// <summary>
    /// Global exception handler. Maps known exceptions to clean HTTP responses
    /// with a uniform JSON shape:
    ///   { "code": "...", "message": "...", "traceId": "...", ["errors": {...}] }
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
            // The client closed the connection mid-request: not a server error. Map to
            // 499 (client closed request), skip the body (no one is listening) and log at
            // Debug so it does not pollute the error dashboards as a 500.
            if (ex is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogDebug("Request aborted by the client.");
                if (!context.Response.HasStarted)
                    context.Response.StatusCode = 499;
                return;
            }

            // FluentValidation: return structured field-level errors.
            if (ex is ValidationException validationEx)
            {
                await WriteValidationErrorAsync(context, validationEx);
                return;
            }

            var (statusCode, code, message) = ex switch
            {
                AuthenticationException => (HttpStatusCode.Unauthorized, "UNAUTHENTICATED", ex.Message),
                // Typed domain exceptions carry their own descriptive Code.
                // NotFoundException is a DomainException, so match it first.
                NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Code, ex.Message),
                DomainException domain => (HttpStatusCode.BadRequest, domain.Code, ex.Message),
                // Legacy fallbacks for throw sites not yet migrated to typed exceptions.
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
            }, JsonOptions);

            await context.Response.WriteAsync(payload);
        }

        private async Task WriteValidationErrorAsync(HttpContext context, ValidationException ex)
        {
            // Group failures by property name -> list of error messages.
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            _logger.LogWarning("Validation failed: {Errors}", string.Join("; ",
                ex.Errors.Select(e => $"{e.PropertyName}={e.ErrorMessage}")));

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                code = "VALIDATION_ERROR",
                message = "Una o varias validaciones han fallado.",
                traceId = context.TraceIdentifier,
                errors
            }, JsonOptions);

            await context.Response.WriteAsync(payload);
        }
    }
}
