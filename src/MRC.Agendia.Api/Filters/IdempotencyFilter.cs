using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Idempotency;
using MRC.Agendia.Domain.Constants;
using JsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace MRC.Agendia.Api.Filters
{
    /// <summary>
    /// Makes an action idempotent when the caller sends an <c>Idempotency-Key</c> header
    /// (#266): a retry of the same request - a double submit, or a resend after a network
    /// timeout - gets the original response back instead of creating a second appointment.
    ///
    /// <para><b>Opt-in.</b> Without the header nothing changes, so existing clients keep
    /// working exactly as before.</para>
    ///
    /// <para><b>How.</b> The key is claimed BEFORE the action runs, so a concurrent twin
    /// finds it in flight rather than both passing a "does it exist?" check and booking
    /// twice. The stored key is scoped to the caller, so two users can never read each
    /// other's response through the same header value. The request payload is hashed with
    /// the endpoint: the same key with a different body is a client bug, and is rejected
    /// instead of silently returning someone else's appointment. A rejected attempt
    /// releases the key so the caller can fix the request and retry it.</para>
    /// </summary>
    public class IdempotencyFilter : IAsyncActionFilter
    {
        public const string HeaderName = "Idempotency-Key";

        private readonly IIdempotencyStore _store;
        private readonly ICurrentUserContext _currentUser;
        private readonly JsonSerializerOptions _jsonOptions;

        public IdempotencyFilter(IIdempotencyStore store,
                                 ICurrentUserContext currentUser,
                                 IOptions<JsonOptions> jsonOptions)
        {
            _store = store;
            _currentUser = currentUser;
            _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues))
            {
                await next();
                return;
            }

            var headerKey = headerValues.ToString().Trim();
            if (headerKey.Length == 0)
            {
                await next();
                return;
            }

            if (headerKey.Length > IdempotencyLimits.MaxHeaderKeyLength)
            {
                context.Result = Error(httpContext, StatusCodes.Status400BadRequest, "IDEMPOTENCY_KEY_INVALID",
                    $"The {HeaderName} header cannot exceed {IdempotencyLimits.MaxHeaderKeyLength} characters.");
                return;
            }

            var cancellationToken = httpContext.RequestAborted;
            var storageKey = BuildStorageKey(headerKey);
            var requestHash = ComputeRequestHash(context);

            var claim = await _store.TryClaimAsync(storageKey, requestHash, cancellationToken);

            switch (claim.Outcome)
            {
                case IdempotencyClaimOutcome.Replay:
                    // Same status and body as the original answer. A replayed 201 carries
                    // no Location header: the header is only produced when the original
                    // result executes, long after this filter runs, and the body already
                    // carries the appointment id.
                    context.Result = new ContentResult
                    {
                        StatusCode = claim.StatusCode,
                        Content = claim.ResponseBody,
                        ContentType = "application/json"
                    };
                    return;

                case IdempotencyClaimOutcome.InProgress:
                    context.Result = Error(httpContext, StatusCodes.Status409Conflict, "IDEMPOTENT_REQUEST_IN_PROGRESS",
                        "An identical request with this idempotency key is still being processed.");
                    return;

                case IdempotencyClaimOutcome.KeyReused:
                    context.Result = Error(httpContext, StatusCodes.Status409Conflict, "IDEMPOTENCY_KEY_REUSED",
                        "This idempotency key was already used for a different request.");
                    return;
            }

            var executed = await next();

            // Only a successful answer is worth replaying. A rejected one (or an unhandled
            // exception, which the middleware turns into a 500) releases the key so the
            // caller can retry once the cause is gone.
            if (executed.Exception is null
                && executed.Result is ObjectResult { Value: not null } result
                && IsSuccess(result.StatusCode))
            {
                await _store.CompleteAsync(
                    storageKey,
                    result.StatusCode ?? StatusCodes.Status200OK,
                    JsonSerializer.Serialize(result.Value, result.Value.GetType(), _jsonOptions),
                    cancellationToken);
            }
            else
            {
                await _store.ReleaseAsync(storageKey, cancellationToken);
            }
        }

        private static bool IsSuccess(int? statusCode) => statusCode is null or (>= 200 and < 300);

        /// <summary>
        /// Scopes the key to the caller: the same header value from two different users
        /// must never read the same record. An unauthenticated caller cannot reach these
        /// endpoints, so the fallback is only a safety net.
        /// </summary>
        private string BuildStorageKey(string headerKey)
        {
            var userId = _currentUser.UserId ?? "anonymous";
            var storageKey = $"{userId}:{headerKey}";
            return storageKey.Length <= IdempotencyLimits.MaxStorageKeyLength
                ? storageKey
                : storageKey[..IdempotencyLimits.MaxStorageKeyLength];
        }

        /// <summary>
        /// Hashes the endpoint plus the bound arguments. Hashing the model rather than the
        /// raw body keeps it stable against formatting differences, and the action
        /// arguments are ordered by the method signature, so the JSON is deterministic.
        /// </summary>
        private string ComputeRequestHash(ActionExecutingContext context)
        {
            var route = context.ActionDescriptor.AttributeRouteInfo?.Template
                        ?? context.ActionDescriptor.DisplayName
                        ?? string.Empty;
            var payload = JsonSerializer.Serialize(context.ActionArguments, _jsonOptions);
            var canonical = $"{context.HttpContext.Request.Method} {route}\n{payload}";

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        }

        // Same body shape ExceptionHandlingMiddleware produces, so clients parse one format.
        private static ObjectResult Error(HttpContext httpContext, int statusCode, string code, string message) =>
            new(new { code, message, traceId = httpContext.TraceIdentifier })
            {
                StatusCode = statusCode
            };
    }
}
