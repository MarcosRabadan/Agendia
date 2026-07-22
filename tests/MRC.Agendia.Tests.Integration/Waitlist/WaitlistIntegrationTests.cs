using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Waitlist
{
    /// <summary>
    /// End-to-end coverage for the waitlist (issue #167): join only when the slot is
    /// full, Staff/non-client cannot use it, and cancelling a booking notifies the
    /// first waiting client.
    ///
    /// The waiting client is resolved by <c>IClientRepository.GetByUserIdAsync</c>, so
    /// the forged token must carry the very same user id stored in Client.UserId.
    /// </summary>
    public class WaitlistIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public WaitlistIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CancelarCitaQueLiberaHueco_AvisaAlClienteEnEspera()
        {
            var owner = await RegisterOwnerAsync("wl-flow");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, "Corte");
            var clientAId = await SeedClientAsync();
            var clientBToken = (await TestProvisioning.ProvisionClientAsync(_client, "wl-b")).Token;

            // Client A's booking fills the slot (employee MaxConcurrent = 1).
            var appointment = await BookAppointmentAsync(owner, clientAId, owner.EmployeeId, service.Id);

            // Client B joins the (now full) slot's waitlist.
            var join = await JoinAsync(clientBToken, new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, owner.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, join.StatusCode);

            // Cancelling A's appointment frees the slot -> B is notified.
            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Appointment/{appointment.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                (await _client.SendAsync(del)).EnsureSuccessStatusCode();
            }

            var mine = await GetMyWaitlistAsync(clientBToken);
            var entry = Assert.Single(mine);
            Assert.Equal(WaitlistStatus.Notified, entry.Status);
        }

        [Fact]
        public async Task Apuntarse_AFranjaConHueco_DevuelveBadRequest()
        {
            var owner = await RegisterOwnerAsync("wl-cap");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, "Corte");
            var clientToken = (await TestProvisioning.ProvisionClientAsync(_client, "wl-c")).Token;

            // No appointment booked -> the slot has capacity -> joining is rejected.
            var response = await JoinAsync(clientToken, new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, EmployeeId: null));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Apuntarse_ComoDueno_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("wl-forbidden");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, "Corte");

            var response = await JoinAsync(owner.Token, new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, EmployeeId: null));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<HttpResponseMessage> JoinAsync(string token, JoinWaitlistDto dto)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/waitlist") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<IReadOnlyList<WaitlistEntryDto>> GetMyWaitlistAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/waitlist/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<WaitlistEntryDto>>();
            Assert.NotNull(list);
            return list!;
        }

        private async Task<AppointmentDto> BookAppointmentAsync(ProvisionedOwner owner, int clientId, int employeeId, int serviceId)
        {
            var start = SlotDate.ToDateTime(SlotTime);
            var dto = new CreateAppointmentDto(clientId, employeeId, serviceId, start, start.AddMinutes(30), Notes: null);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(created);
            return created!;
        }

        /// <summary>
        /// Client A only needs to hold the booking that fills the slot, so it is
        /// seeded straight into the database with no user account.
        /// </summary>
        private async Task<int> SeedClientAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var client = new Client { Name = "Cliente A", Phone = "600111222", Email = "a@test.local" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client.Id;
        }

        private async Task GenerateScheduleAsync(ProvisionedOwner owner)
        {
            var request = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: Year,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(Year, 1, 1),
                        EffectiveTo: new DateOnly(Year, 12, 31),
                        IsDefault: true,
                        WeeklySlots: Enum.GetValues<DayOfWeek>()
                            .Select(d => new CreateWeeklyTimeSlotDto(d, new TimeOnly(9, 0), new TimeOnly(18, 0), TimeSlotType.Regular))
                            .ToList()),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null);

            using var gen = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/schedules/generate")
            {
                Content = JsonContent.Create(request)
            };
            gen.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            (await _client.SendAsync(gen)).EnsureSuccessStatusCode();
        }

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner, string name)
        {
            var dto = new CreateServiceDto(owner.Business.Id, name, null, 30, 20m);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        /// <summary>
        /// Creates a client row bound to a Harmony user id and returns a token for
        /// that same user id, so the waitlist can resolve the client from the JWT.
        /// </summary>
        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
