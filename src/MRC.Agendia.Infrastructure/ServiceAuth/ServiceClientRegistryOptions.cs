namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>Binds the <c>ServiceClients</c> array of trusted M2M clients.</summary>
    public class ServiceClientRegistryOptions
    {
        /// <summary>The list of configured service clients (may be empty).</summary>
        public List<ServiceClientOptions> ServiceClients { get; set; } = new();
    }
}
