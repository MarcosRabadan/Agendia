using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Data-protection token provider with a short (1-hour by default) lifespan,
    /// wired to password reset via <c>options.Tokens.PasswordResetTokenProvider</c>
    /// in AuthenticationSetup. IOptions covariance lets the derived options flow
    /// into the base provider constructor.
    /// </summary>
    public class ShortLivedTokenProvider<TUser> : DataProtectorTokenProvider<TUser>
        where TUser : class
    {
        public ShortLivedTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<ShortLivedTokenProviderOptions> options,
            ILogger<DataProtectorTokenProvider<TUser>> logger)
            : base(dataProtectionProvider, options, logger)
        {
        }
    }
}
