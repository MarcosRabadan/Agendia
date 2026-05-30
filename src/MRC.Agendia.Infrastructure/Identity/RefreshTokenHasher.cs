using System.Security.Cryptography;
using System.Text;

namespace MRC.Agendia.Infrastructure.Identity
{
    /// <summary>
    /// Hashes refresh tokens for storage. The cleartext token is returned to the
    /// client; only its SHA-256 (lowercase hex) is persisted, so a DB leak does not
    /// hand out reusable tokens. A plain SHA-256 (not a slow KDF) is enough here: the
    /// token is a 64-byte CSPRNG value, so it is not guessable/brute-forceable. The
    /// lowercase-hex output is also collation-safe (no case-insensitive collisions),
    /// unlike the case-sensitive Base64 cleartext stored before.
    /// </summary>
    public static class RefreshTokenHasher
    {
        public static string Hash(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
