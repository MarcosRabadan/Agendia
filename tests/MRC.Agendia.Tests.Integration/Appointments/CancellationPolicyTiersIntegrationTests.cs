using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for the tiered cancellation policy (#270): the owner sets the
    /// tiers, a client cancelling inside a penalised tier gets the cancellation through
    /// WITH the applied tier reported, a client inside the blocked tier is rejected with
    /// the usual code, and a business that never sets tiers keeps the old single-threshold
    /// behaviour.
    /// </summary>
    public class CancellationPolicyTiersIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CancellationPolicyTiersIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Owner_sets_the_tiers_and_reads_them_back()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "tiers-crud");

            var saved = await PutPolicyAsync(owner.Token, owner.Business.Id, Tiers());

            Assert.Equal(3, saved.Count);
            // Ordered from the most notice to the least.
            Assert.Equal(new[] { 24, 4, 0 }, saved.Select(t => t.MinHoursBefore).ToArray());
            Assert.Equal(50m, saved.Single(t => t.MinHoursBefore == 4).PenaltyValue);

            var read = await GetPolicyAsync(owner.Token, owner.Business.Id);
            Assert.Equal(saved.Select(t => t.MinHoursBefore), read.Select(t => t.MinHoursBefore));
        }

        [Fact]
        public async Task A_policy_without_a_zero_tier_is_rejected()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "tiers-invalid");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                PolicyUrl(owner.Business.Id), owner.Token,
                new UpdateCancellationPolicyDto(new List<CancellationPolicyTierDto>
                {
                    new(24, CancellationPenaltyKind.None),
                    new(4, CancellationPenaltyKind.Percentage, 50m)
                }));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Staff_of_another_business_cannot_set_the_policy()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "tiers-owner");
            var stranger = await TestProvisioning.ProvisionOwnerAsync(_client, "tiers-stranger");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                PolicyUrl(owner.Business.Id), stranger.Token,
                new UpdateCancellationPolicyDto(Tiers()));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Cancelling_inside_a_penalised_tier_succeeds_and_reports_it()
        {
            var (setup, clientAccount, appointment) = await BookForTomorrowAsync("tiers-penalty");
            // Tiers relative to an appointment ~24h away: free 48h ahead, 50% from 1h.
            await PutPolicyAsync(setup.OwnerToken, setup.BusinessId, new List<CancellationPolicyTierDto>
            {
                new(48, CancellationPenaltyKind.None),
                new(1, CancellationPenaltyKind.Percentage, 50m),
                new(0, CancellationPenaltyKind.NotAllowed)
            });

            var response = await CancelAsync(clientAccount.Token, appointment);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cancelled = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(AppointmentStatus.Cancelled, cancelled!.Status);
            Assert.NotNull(cancelled.AppliedCancellationTier);
            Assert.Equal(1, cancelled.AppliedCancellationTier!.MinHoursBefore);
            Assert.Equal(CancellationPenaltyKind.Percentage, cancelled.AppliedCancellationTier.PenaltyKind);
            Assert.Equal(50m, cancelled.AppliedCancellationTier.PenaltyValue);
        }

        [Fact]
        public async Task Cancelling_inside_the_blocked_tier_is_rejected()
        {
            var (setup, clientAccount, appointment) = await BookForTomorrowAsync("tiers-blocked");
            // Everything under 48h of notice is blocked; the appointment is ~24h away.
            await PutPolicyAsync(setup.OwnerToken, setup.BusinessId, new List<CancellationPolicyTierDto>
            {
                new(48, CancellationPenaltyKind.None),
                new(0, CancellationPenaltyKind.NotAllowed)
            });

            var response = await CancelAsync(clientAccount.Token, appointment);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("CANCELLATION_WINDOW_ELAPSED", error!.Code);
        }

        [Fact]
        public async Task Without_tiers_the_cancellation_stays_as_it_was()
        {
            var (_, clientAccount, appointment) = await BookForTomorrowAsync("tiers-legacy");

            // No policy configured at all: the business has no window either, so the
            // client cancels freely and no tier is reported.
            var response = await CancelAsync(clientAccount.Token, appointment);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cancelled = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(AppointmentStatus.Cancelled, cancelled!.Status);
            Assert.Null(cancelled.AppliedCancellationTier);
        }

        // ----- Helpers -----

        private static List<CancellationPolicyTierDto> Tiers() => new()
        {
            new(24, CancellationPenaltyKind.None),
            new(4, CancellationPenaltyKind.Percentage, 50m),
            new(0, CancellationPenaltyKind.NotAllowed)
        };

        private static string PolicyUrl(Guid businessId) => $"/api/businesses/{businessId}/cancellation-policy";

        /// <summary>
        /// Books an appointment for tomorrow morning (so the tiers can be placed around a
        /// known distance) on behalf of the client, who then owns it and can self-cancel.
        /// </summary>
        private async Task<(BookableBusiness Setup, ProvisionedClient Client, AppointmentDto Appointment)>
            BookForTomorrowAsync(string slug)
        {
            var tomorrow = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(1));
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, slug, tomorrow.Year);
            var clientAccount = TestProvisioning.ProvisionClient(slug);

            var start = tomorrow.ToDateTime(new TimeOnly(10, 0));
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, clientAccount.Token,
                new CreateAppointmentDto(clientAccount.UserId, setup.EmployeeId, setup.Service.Id,
                    start, start.AddMinutes(30), null));
            response.EnsureSuccessStatusCode();

            return (setup, clientAccount, (await response.Content.ReadFromJsonAsync<AppointmentDto>())!);
        }

        private Task<HttpResponseMessage> CancelAsync(string token, AppointmentDto appointment)
            => BookableBusinessFactory.SendAsync(_client, HttpMethod.Put, $"/api/Appointment/{appointment.Id}", token,
                new UpdateAppointmentDto(appointment.Id, appointment.ClientUserId, appointment.EmployeeId,
                    appointment.ServiceId, appointment.StartDate, appointment.EndDate,
                    AppointmentStatus.Cancelled, appointment.Notes));

        private async Task<IReadOnlyList<CancellationPolicyTierDto>> PutPolicyAsync(string token,
                                                                                    Guid businessId,
                                                                                    IReadOnlyList<CancellationPolicyTierDto> tiers)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put, PolicyUrl(businessId), token,
                new UpdateCancellationPolicyDto(tiers));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<List<CancellationPolicyTierDto>>())!;
        }

        private async Task<IReadOnlyList<CancellationPolicyTierDto>> GetPolicyAsync(string token, Guid businessId)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, PolicyUrl(businessId), token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<List<CancellationPolicyTierDto>>())!;
        }
    }
}
