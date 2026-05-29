using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
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
    /// </summary>
    public class WaitlistIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private const string OwnerPassword = "Owner1234!";
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
            var (employeeId, clientAId) = await SeedEmployeeAndClientAsync(owner.Business.Id);
            var clientBToken = await RegisterClientAndGetTokenAsync("wl-b");

            // Client A's booking fills the slot (employee MaxConcurrent = 1).
            var appointment = await BookAppointmentAsync(owner, clientAId, employeeId, service.Id);

            // Client B joins the (now full) slot's waitlist.
            var join = await JoinAsync(clientBToken, new JoinWaitlistDto(owner.Business.Id, service.Id, SlotDate, SlotTime, employeeId));
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
            await SeedEmployeeAndClientAsync(owner.Business.Id);
            var clientToken = await RegisterClientAndGetTokenAsync("wl-c");

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

        private async Task<AppointmentDto> BookAppointmentAsync(RegisteredOwner owner, int clientId, int employeeId, int serviceId)
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

        private async Task<(int EmployeeId, int ClientId)> SeedEmployeeAndClientAsync(int businessId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var employeeId = (await db.Employees.FirstAsync(e => e.BusinessId == businessId)).Id;
            var client = new Client { Name = "Cliente A", Phone = "600111222", Email = "a@test.local" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return (employeeId, client.Id);
        }

        private async Task GenerateScheduleAsync(RegisteredOwner owner)
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

        private async Task<ServiceDto> CreateServiceAsAsync(RegisteredOwner owner, string name)
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

        private async Task<string> RegisterClientAndGetTokenAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var dto = new RegisterClientDto($"{slug}-{unique}@test.local", "Client1234!", $"Cliente {slug}", "600999888");
            var response = await _client.PostAsJsonAsync("/api/auth/register/client", dto);
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);
            return auth!.AccessToken;
        }

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{unique}@test.local";
            var businessName = $"{slug}-{unique}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{unique}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessPublicDto Business);
    }
}
