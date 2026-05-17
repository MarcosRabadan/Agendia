using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Rate limiting for sensitive auth endpoints. Partitioned by client IP.
    ///
    /// Policies:
    ///   - "auth-login"    : 5  / IP / minute
    ///   - "auth-refresh"  : 10 / IP / minute
    ///   - "auth-register" : 3  / IP / hour
    /// </summary>
    public static class RateLimitingSetup
    {
        public const string LoginPolicy = "auth-login";
        public const string RefreshPolicy = "auth-refresh";
        public const string RegisterPolicy = "auth-register";

        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString();
                    }
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"code\":\"RATE_LIMITED\",\"message\":\"Demasiadas peticiones. Intentalo de nuevo mas tarde.\"}",
                        cancellationToken);
                };

                options.AddPolicy(LoginPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy(RefreshPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));

                options.AddPolicy(RegisterPolicy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromHours(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
            });

            return services;
        }

        private static string GetPartitionKey(HttpContext httpContext)
            => httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
