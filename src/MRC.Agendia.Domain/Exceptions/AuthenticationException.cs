namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// Thrown when authentication itself fails (bad credentials, locked account,
    /// expired/revoked refresh token, deactivated user). Maps to HTTP 401 in
    /// <c>ExceptionHandlingMiddleware</c>.
    ///
    /// Distinct from <see cref="UnauthorizedAccessException"/>, which models
    /// "authenticated but not allowed to touch this resource" (HTTP 403).
    /// </summary>
    public class AuthenticationException : Exception
    {
        public AuthenticationException(string message) : base(message)
        {
        }

        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
