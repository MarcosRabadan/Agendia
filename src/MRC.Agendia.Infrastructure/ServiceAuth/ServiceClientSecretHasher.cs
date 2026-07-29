using System.Security.Cryptography;

namespace MRC.Agendia.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Hashes and verifies service-client secrets with PBKDF2 (HMAC-SHA256).
    /// Stored format: <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>.
    /// Verification is constant-time. Machine secrets are high-entropy, but salting
    /// plus stretching is kept as defense in depth and to mirror how user passwords
    /// used to be stored.
    ///
    /// <para><b>Generating a stored hash:</b> call <see cref="Hash"/> with the chosen
    /// plaintext secret (e.g. from a throwaway test or a small script) and paste the
    /// result into the <c>ClientSecretHash</c> config value. See
    /// <c>docs/service-auth-contract.md</c>.</para>
    /// </summary>
    public static class ServiceClientSecretHasher
    {
        private const string Prefix = "pbkdf2-sha256";
        private const int DefaultIterations = 100_000;
        private const int SaltSize = 16; // bytes
        private const int KeySize = 32;  // bytes (256-bit derived key)

        /// <summary>Produces a self-describing, salted PBKDF2 hash of <paramref name="secret"/>.</summary>
        public static string Hash(string secret)
        {
            ArgumentNullException.ThrowIfNull(secret);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(secret, salt, DefaultIterations, HashAlgorithmName.SHA256, KeySize);

            return string.Join('$',
                Prefix,
                DefaultIterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        /// <summary>
        /// Returns true when <paramref name="secret"/> matches <paramref name="stored"/>.
        /// A malformed stored value returns false rather than throwing.
        /// </summary>
        public static bool Verify(string secret, string stored)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(stored))
                return false;

            var parts = stored.Split('$');
            if (parts.Length != 4 || parts[0] != Prefix)
                return false;

            if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
                return false;

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
