using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Common
{
    /// <summary>
    /// Verifies the middleware surfaces specific, machine-readable error codes
    /// (issue #60) instead of the generic ones.
    ///
    /// This used to assert on DUPLICATE_EMAIL from the registration endpoint. Both
    /// are gone with the move to Harmony, so the guard is now anchored on a domain
    /// rule Agendia still owns: two schedule overrides on the same business+date.
    /// The point is the contract itself - a DomainException must reach the client as
    /// a 400 carrying its own code, not as a generic BAD_REQUEST or a 500.
    /// </summary>
    public class ErrorCodesTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ErrorCodesTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task OverrideDuplicado_DevuelveCodeScheduleOverrideConflict()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "errcode");

            var first = await CreateOverrideAsync(owner);
            first.EnsureSuccessStatusCode();

            var second = await CreateOverrideAsync(owner);

            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
            var body = await second.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"SCHEDULE_OVERRIDE_CONFLICT\"", body);
        }

        [Fact]
        public async Task PeticionInvalida_DevuelveCodeValidationError()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "errval");

            // An unset date fails FluentValidation (RuleFor(Date).NotEqual(default)),
            // which the middleware reports as VALIDATION_ERROR with a per-field map.
            var response = await CreateOverrideAsync(owner, date: new DateOnly());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"code\":\"VALIDATION_ERROR\"", body);
            Assert.Contains("\"errors\"", body);
        }

        private async Task<HttpResponseMessage> CreateOverrideAsync(ProvisionedOwner owner, DateOnly? date = null)
        {
            var dto = new CreateScheduleOverrideDto(BusinessId: owner.Business.Id,
                                                    Date: date ?? new DateOnly(2035, 8, 15),
                                                    OverrideType: ScheduleOverrideType.Closed,
                                                    Reason: "Cierre",
                                                    CustomSlots: null);

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"/api/businesses/{owner.Business.Id}/schedules/overrides")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            return await _client.SendAsync(request);
        }
    }
}
