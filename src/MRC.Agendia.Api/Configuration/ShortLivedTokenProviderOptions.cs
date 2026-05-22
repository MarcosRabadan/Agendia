using Microsoft.AspNetCore.Identity;

namespace MRC.Agendia.Api.Configuration
{
    /// <summary>
    /// Options for the short-lived data-protection token provider used by
    /// password reset. Kept separate from the default
    /// <see cref="DataProtectionTokenProviderOptions"/> so the reset token can
    /// have a 1-hour lifespan while email confirmation keeps a longer one.
    ///
    /// Lives in the Api project because <see cref="DataProtectorTokenProvider{TUser}"/>
    /// ships in the ASP.NET Core shared framework, not available to the
    /// Infrastructure class library. It is wired up in AuthenticationSetup.
    /// </summary>
    public class ShortLivedTokenProviderOptions : DataProtectionTokenProviderOptions
    {
        public ShortLivedTokenProviderOptions()
        {
            Name = "PasswordResetShortLived";
            TokenLifespan = TimeSpan.FromHours(1);
        }
    }
}
