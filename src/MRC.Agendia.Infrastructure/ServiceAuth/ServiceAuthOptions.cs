namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>Settings for the service (client-credentials) token, bound from <c>ServiceAuth</c>.</summary>
    public class ServiceAuthOptions
    {
        public const string SectionName = "ServiceAuth";

        /// <summary>Lifetime of an issued service token, in minutes (default 15).</summary>
        public int TokenLifetimeMinutes { get; set; } = 15;
    }
}
