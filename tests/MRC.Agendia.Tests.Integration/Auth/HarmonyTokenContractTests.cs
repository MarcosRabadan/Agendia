using System.Net;
using System.Net.Http.Headers;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// Pins the token contract between Harmony (the identity service that issues
    /// tokens) and Agendia (which only validates them).
    ///
    /// These assertions are deliberately blunt, because getting the claim mapping
    /// wrong fails in the most confusing way possible: the token validates, the
    /// request is authenticated, and then every authorization check returns 403
    /// because ICurrentUserContext.UserId came back null. If one of these tests
    /// breaks, the integration with Harmony is broken - do not "fix" it by relaxing
    /// the assertion.
    /// </summary>
    public class HarmonyTokenContractTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HarmonyTokenContractTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Request_without_token_is_rejected()
        {
            var response = await _client.GetAsync("/api/Client?page=1&pageSize=10");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Role_claim_from_harmony_grants_access_to_a_role_gated_endpoint()
        {
            var response = await GetAsync("/api/Client?page=1&pageSize=10",
                TestTokenFactory.Create("harmony-admin-1", Roles.Admin));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Token_without_the_required_role_is_forbidden()
        {
            var response = await GetAsync("/api/Client?page=1&pageSize=10",
                TestTokenFactory.Create("harmony-client-1", Roles.Client));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Sub_claim_is_read_as_the_caller_identity_for_ownership_checks()
        {
            // ProvisionOwnerAsync creates the business as Admin and then creates an
            // employee using the OWNER's token. That second call only succeeds if
            // EnsureCanManageBusinessResourcesAsync matched Business.OwnerUserId
            // against the caller's "sub" claim, so it proves the mapping end to end.
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "contract");

            Assert.NotEqual(0, owner.EmployeeId);
            Assert.Equal(owner.OwnerUserId, owner.Business.OwnerUserId);
        }

        [Fact]
        public async Task Token_signed_with_a_different_key_is_rejected()
        {
            // A token Agendia did not have the shared secret for must not pass, even
            // though its claims and issuer/audience are otherwise well formed.
            var forged = TestTokenFactory.CreateCustom("harmony-admin-1",
                roles: new[] { Roles.Admin },
                signingKey: "a-completely-different-key-that-agendia-does-not-know-0123456789");

            var response = await GetAsync("/api/Client?page=1&pageSize=10", forged);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Token_from_a_different_issuer_is_rejected()
        {
            var token = TestTokenFactory.CreateCustom("harmony-admin-1",
                roles: new[] { Roles.Admin },
                issuer: "https://not-harmony.example");

            var response = await GetAsync("/api/Client?page=1&pageSize=10", token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Token_for_a_different_audience_is_rejected()
        {
            // Stops a token minted for a SIBLING Harmony microservice, signed with the
            // same shared key, from being replayed against Agendia.
            var token = TestTokenFactory.CreateCustom("harmony-admin-1",
                roles: new[] { Roles.Admin },
                audience: "some-other-harmony-service");

            var response = await GetAsync("/api/Client?page=1&pageSize=10", token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Expired_token_is_rejected()
        {
            // Well beyond the 1-minute clock skew allowance.
            var token = TestTokenFactory.CreateCustom("harmony-admin-1",
                roles: new[] { Roles.Admin },
                expires: DateTime.UtcNow.AddMinutes(-30));

            var response = await GetAsync("/api/Client?page=1&pageSize=10", token);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Token_without_sub_cannot_reach_tenant_scoped_data()
        {
            // A malformed token that authenticates but carries no identity must not
            // be treated as "unscoped" by the multi-tenant filter. It resolves to no
            // business at all, so an owner-scoped listing comes back empty.
            var token = TestTokenFactory.CreateCustom(userId: null,
                roles: new[] { Roles.BusinessOwner });

            var response = await GetAsync("/api/Employee?page=1&pageSize=50", token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<HttpResponseMessage> GetAsync(string url, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

    }
}
