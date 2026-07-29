using MRC.Agendia.Infrastructure.ServiceAuth;

namespace MRC.Agendia.Tests.Unit.Infrastructure.ServiceAuth
{
    /// <summary>
    /// Unit tests for the PBKDF2 service-secret hasher: a correct secret verifies,
    /// a wrong one does not, each hash is uniquely salted, and malformed stored
    /// values fail closed instead of throwing.
    /// </summary>
    public class ServiceClientSecretHasherTests
    {
        [Fact]
        public void Hash_then_Verify_with_same_secret_succeeds()
        {
            var hash = ServiceClientSecretHasher.Hash("super-secret-123");

            Assert.True(ServiceClientSecretHasher.Verify("super-secret-123", hash));
        }

        [Fact]
        public void Verify_with_wrong_secret_fails()
        {
            var hash = ServiceClientSecretHasher.Hash("super-secret-123");

            Assert.False(ServiceClientSecretHasher.Verify("wrong-secret", hash));
        }

        [Fact]
        public void Hash_is_salted_so_the_same_secret_hashes_differently_each_time()
        {
            var first = ServiceClientSecretHasher.Hash("same-secret");
            var second = ServiceClientSecretHasher.Hash("same-secret");

            Assert.NotEqual(first, second);
            Assert.True(ServiceClientSecretHasher.Verify("same-secret", first));
            Assert.True(ServiceClientSecretHasher.Verify("same-secret", second));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-valid-format")]
        [InlineData("pbkdf2-sha256$notanumber$c2FsdA==$aGFzaA==")]
        [InlineData("pbkdf2-sha256$100000$@@@$aGFzaA==")]
        public void Verify_with_malformed_stored_value_returns_false(string stored)
        {
            Assert.False(ServiceClientSecretHasher.Verify("secret", stored));
        }
    }
}
