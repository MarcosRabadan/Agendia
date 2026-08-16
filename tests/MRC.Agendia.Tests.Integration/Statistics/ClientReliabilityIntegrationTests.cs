using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Statistics
{
    /// <summary>
    /// End-to-end coverage for the client reliability endpoint (#267): the metrics count
    /// only this client, only this business and only the requested window, they are
    /// visible to the business staff and not to a client, and the payload carries no
    /// profile data (Agendia only owns the Harmony user id).
    ///
    /// Appointments are seeded straight into the store because the booking endpoint
    /// (rightly) refuses dates in the past, and a reliability record is made of elapsed
    /// appointments.
    /// </summary>
    public class ClientReliabilityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ClientReliabilityIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Staff_sees_the_clients_record_for_the_window()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-staff");
            var service = await BookableBusinessFactory.CreateServiceAsync(_client, owner, 30);
            var clientUserId = BookableBusinessFactory.CounterClientUserId();
            var otherClientUserId = BookableBusinessFactory.CounterClientUserId();

            await SeedAsync(owner.EmployeeId, service.Id,
                (clientUserId, -10, AppointmentStatus.Completed),
                (clientUserId, -9, AppointmentStatus.NoShow),
                (clientUserId, -8, AppointmentStatus.Cancelled),
                // Outside the 90-day window, and someone else's: neither must count.
                (clientUserId, -200, AppointmentStatus.NoShow),
                (otherClientUserId, -7, AppointmentStatus.NoShow));

            var result = await GetReliabilityAsync(owner.Token, owner.Business.Id, clientUserId);

            Assert.Equal(3, result.Total);
            Assert.Equal(1, result.Completed);
            Assert.Equal(1, result.NoShow);
            Assert.Equal(1, result.Cancelled);
            // No-show over completed + no-show, cancellations over the total.
            Assert.Equal(0.5, result.NoShowRate);
            Assert.Equal(Math.Round(1d / 3, 4), result.CancellationRate);
            Assert.Equal(clientUserId, result.ClientUserId);
            Assert.Equal(owner.Business.Id, result.BusinessId);
        }

        [Fact]
        public async Task Window_can_be_narrowed_with_the_days_parameter()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-window");
            var service = await BookableBusinessFactory.CreateServiceAsync(_client, owner, 30);
            var clientUserId = BookableBusinessFactory.CounterClientUserId();

            await SeedAsync(owner.EmployeeId, service.Id,
                (clientUserId, -3, AppointmentStatus.Completed),
                (clientUserId, -40, AppointmentStatus.NoShow));

            var narrow = await GetReliabilityAsync(owner.Token, owner.Business.Id, clientUserId, days: 7);

            Assert.Equal(1, narrow.Total);
            Assert.Equal(0, narrow.NoShow);
        }

        [Fact]
        public async Task A_client_cannot_read_a_reliability_record()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-forbidden");
            var clientAccount = TestProvisioning.ProvisionClient("rel");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                Url(owner.Business.Id, clientAccount.UserId), clientAccount.Token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Staff_of_another_business_is_rejected()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-owner");
            var stranger = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-stranger");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                Url(owner.Business.Id, BookableBusinessFactory.CounterClientUserId()), stranger.Token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task An_out_of_range_window_is_rejected()
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(_client, "rel-range");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                Url(owner.Business.Id, BookableBusinessFactory.CounterClientUserId()) + "?days=0", owner.Token);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ----- Helpers -----

        private static string Url(Guid businessId, string clientUserId) =>
            $"/api/businesses/{businessId}/clients/{clientUserId}/reliability";

        private async Task<ClientReliabilityDto> GetReliabilityAsync(string token,
                                                                     Guid businessId,
                                                                     string clientUserId,
                                                                     int? days = null)
        {
            var url = Url(businessId, clientUserId) + (days is null ? string.Empty : $"?days={days}");
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, url, token);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<ClientReliabilityDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        /// <summary>Seeds elapsed appointments as (client, days ago, outcome).</summary>
        private async Task SeedAsync(Guid employeeId,
                                     Guid serviceId,
                                     params (string ClientUserId, int DaysAgo, AppointmentStatus Status)[] appointments)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            foreach (var (clientUserId, daysAgo, status) in appointments)
            {
                var start = DateTime.Now.Date.AddDays(daysAgo).AddHours(10);
                db.Appointments.Add(new Appointment
                {
                    ClientUserId = clientUserId,
                    EmployeeId = employeeId,
                    ServiceId = serviceId,
                    StartDate = start,
                    EndDate = start.AddMinutes(30),
                    Status = status
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
