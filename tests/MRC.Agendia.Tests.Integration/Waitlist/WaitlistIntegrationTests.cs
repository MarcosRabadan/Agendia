using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Waitlist
{
    /// <summary>
    /// End-to-end coverage for the waitlist (issue #167): join only when the slot is
    /// full, Staff/non-client cannot use it, and cancelling a booking notifies the
    /// first waiting client.
    ///
    /// The waiting client is identified by the token's "sub" (its Harmony user id),
    /// which is stored directly on the waitlist entry (no Client entity).
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
            var service = await CreateServiceAsAsync(owner);
            var clientAUserId = ClientAUserId();
            var clientBToken = TestProvisioning.ProvisionClient("wl-b").Token;

            // Client A's booking fills the slot (employee MaxConcurrent = 1).
            var appointment = await BookAppointmentAsync(owner, clientAUserId, owner.EmployeeId, service.Id);

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

        /// <summary>
        /// The one that reproduces #350. Joining is allowed whenever the slot is FULL, and
        /// fullness is measured by overlap: a 10:00-11:00 class fills 10:30 too, so the API lets
        /// you queue there. The notification then matched candidates by exact start time, so that
        /// entry sat in a queue it could never be called from - not even, as here, when the
        /// teacher ends up with the whole day free.
        /// </summary>
        [Fact]
        public async Task EsperarAUnaHoraQueSolapa_RecibeElAvisoAlLiberarse()
        {
            var owner = await RegisterOwnerAsync("wl-overlap");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, durationMinutes: 60);
            var clientToken = TestProvisioning.ProvisionClient("wl-overlap-b").Token;

            // 10:00-11:00 fills the employee (MaxConcurrent = 1) for the whole hour.
            var appointment = await BookAppointmentAsync(owner, ClientAUserId(), owner.EmployeeId, service.Id, durationMinutes: 60);

            // ...so 10:30 is full as well, and queueing there is accepted.
            var join = await JoinAsync(clientToken,
                new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, new TimeOnly(10, 30), owner.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, join.StatusCode);

            await CancelAsync(owner, appointment.Id);

            var entry = Assert.Single(await GetMyWaitlistAsync(clientToken));
            Assert.Equal(WaitlistStatus.Notified, entry.Status);
            Assert.NotNull(entry.HoldUntil);
        }

        /// <summary>
        /// Control: overlapping the freed window makes you a candidate, not a winner. Capacity is
        /// still the authority, and here the next class keeps 10:30 blocked.
        /// </summary>
        [Fact]
        public async Task EsperarAUnaHoraQueSolapa_PeroSigueLlena_NoRecibeAviso()
        {
            var owner = await RegisterOwnerAsync("wl-overlap-full");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, durationMinutes: 60);
            var clientToken = TestProvisioning.ProvisionClient("wl-overlap-full-b").Token;

            var first = await BookAppointmentAsync(owner, ClientAUserId(), owner.EmployeeId, service.Id, durationMinutes: 60);
            await BookAppointmentAsync(owner, ClientAUserId(), owner.EmployeeId, service.Id,
                durationMinutes: 60, at: new TimeOnly(11, 0));

            var join = await JoinAsync(clientToken,
                new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, new TimeOnly(10, 30), owner.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, join.StatusCode);

            await CancelAsync(owner, first.Id);

            // 10:30-11:30 still runs into the 11:00 class: no false "there is a spot".
            var entry = Assert.Single(await GetMyWaitlistAsync(clientToken));
            Assert.Equal(WaitlistStatus.Waiting, entry.Status);
        }

        /// <summary>
        /// One freed seat is one notification, and it goes by queue order - which now spans
        /// different start times. Whoever queued first wins even though the other one asked for
        /// exactly the hour that was freed.
        /// </summary>
        [Fact]
        public async Task ConVariosEnEspera_AvisaSoloAlPrimeroDeLaCola()
        {
            var owner = await RegisterOwnerAsync("wl-overlap-fifo");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, durationMinutes: 60);
            var firstInQueue = TestProvisioning.ProvisionClient("wl-fifo-1").Token;
            var secondInQueue = TestProvisioning.ProvisionClient("wl-fifo-2").Token;

            var appointment = await BookAppointmentAsync(owner, ClientAUserId(), owner.EmployeeId, service.Id, durationMinutes: 60);

            var joinFirst = await JoinAsync(firstInQueue,
                new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, new TimeOnly(10, 30), owner.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, joinFirst.StatusCode);
            var joinSecond = await JoinAsync(secondInQueue,
                new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, owner.EmployeeId));
            Assert.Equal(HttpStatusCode.OK, joinSecond.StatusCode);

            await CancelAsync(owner, appointment.Id);

            Assert.Equal(WaitlistStatus.Notified, Assert.Single(await GetMyWaitlistAsync(firstInQueue)).Status);
            Assert.Equal(WaitlistStatus.Waiting, Assert.Single(await GetMyWaitlistAsync(secondInQueue)).Status);
        }

        [Fact]
        public async Task Apuntarse_AFranjaConHueco_DevuelveBadRequest()
        {
            var owner = await RegisterOwnerAsync("wl-cap");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner);
            var clientToken = TestProvisioning.ProvisionClient("wl-c").Token;

            // No appointment booked -> the slot has capacity -> joining is rejected.
            var response = await JoinAsync(clientToken, new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, EmployeeId: null));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Apuntarse_ComoDueno_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("wl-forbidden");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner);

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

        /// <summary>Cancels a booking through the API, which is what frees the slot.</summary>
        private async Task CancelAsync(ProvisionedOwner owner, Guid appointmentId)
        {
            using var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Appointment/{appointmentId}");
            del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            (await _client.SendAsync(del)).EnsureSuccessStatusCode();
        }

        private async Task<AppointmentDto> BookAppointmentAsync(ProvisionedOwner owner,
                                                                string clientUserId,
                                                                Guid employeeId,
                                                                Guid serviceId,
                                                                int durationMinutes = 30,
                                                                TimeOnly? at = null)
        {
            var start = SlotDate.ToDateTime(at ?? SlotTime);
            var dto = new CreateAppointmentDto(clientUserId, employeeId, serviceId, start, start.AddMinutes(durationMinutes), Notes: null);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(created);
            return created!;
        }

        /// <summary>
        /// Client A only needs a user id to hold the booking that fills the slot; the
        /// appointment stores it directly (no Client entity anymore).
        /// </summary>
        private static string ClientAUserId() => $"harmony-wl-a-{Guid.NewGuid():N}";

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

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner, int durationMinutes = 30)
        {
            var dto = new CreateServiceDto(owner.Business.Id, durationMinutes);
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
