using MRC.Agendia.Infrastructure.Identity;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Identity
{
    public class RefreshTokenHasherTests
    {
        [Fact]
        public void Hash_IsDeterministic_LowercaseHex64()
        {
            var hash = RefreshTokenHasher.Hash("a-refresh-token");

            Assert.Equal(64, hash.Length);
            Assert.Equal(RefreshTokenHasher.Hash("a-refresh-token"), hash); // deterministic
            Assert.Matches("^[0-9a-f]{64}$", hash);
        }

        [Fact]
        public void Hash_DifferentInputs_ProduceDifferentHashes()
        {
            Assert.NotEqual(RefreshTokenHasher.Hash("token-a"), RefreshTokenHasher.Hash("token-b"));
        }
    }
}
