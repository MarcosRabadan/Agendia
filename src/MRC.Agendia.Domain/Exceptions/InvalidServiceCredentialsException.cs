namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// A service-to-service (client-credentials) authentication attempt failed:
    /// the clientId is unknown, the secret does not match, or the client is
    /// disabled. The message is deliberately uniform across all three causes so
    /// the token endpoint does not leak which clientIds exist. Maps to HTTP 401.
    /// </summary>
    public class InvalidServiceCredentialsException : DomainException
    {
        public override string Code => "INVALID_SERVICE_CREDENTIALS";

        public InvalidServiceCredentialsException()
            : base("Credenciales de servicio invalidas.")
        {
        }
    }
}
