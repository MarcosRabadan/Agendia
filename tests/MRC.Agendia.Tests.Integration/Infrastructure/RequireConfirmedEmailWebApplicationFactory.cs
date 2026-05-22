using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Same as <see cref="CustomWebApplicationFactory"/> but with
    /// <c>Auth:RequireConfirmedEmail = true</c>. The flag is injected via an
    /// in-memory configuration source (not an environment variable) so it stays
    /// scoped to this host and does not leak into other test classes running in
    /// parallel. The flag is read at request time, so this is applied in time.
    /// </summary>
    public class RequireConfirmedEmailWebApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:RequireConfirmedEmail"] = "true"
                });
            });
        }
    }
}
