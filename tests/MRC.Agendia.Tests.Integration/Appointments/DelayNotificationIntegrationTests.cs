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
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for the delay alert (issue #168): Staff-only endpoint,
    /// and notifying the upcoming appointments of "today". The happy path seeds an
    /// appointment later today; it is skipped near midnight when there is no room
    /// for a future same-day slot.
    /// </summary>
    public class DelayNotificationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string OwnerPassword = "Owner1234!";

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DelayNotificationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task NotifyDelay_ComoCliente_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("delay-forbidden");
            var clientToken = await RegisterClientAndGetTokenAsync("delay-cli");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/notify-delay")
            {
                Content = JsonContent.Create(new NotifyDelayDto(EmployeeId: null, DelayMinutes: 20, MaxAppointments: null))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task NotifyDelay_SinCitas_DevuelveCero()
        {
            var owner = await RegisterOwnerAsync("delay-empty");
            await GenerateAllDayScheduleAsync(owner);

            var body = await NotifyDelayAsync(owner, new NotifyDelayDto(null, 15, null));

            Assert.Equal(0, body.Notified);
        }

        [SkippableFact]
        public async Task NotifyDelay_ConCitaFuturaHoy_NotificaAlMenosUna()
        {
            var owner = await RegisterOwnerAsync("delay-ok");
            await GenerateAllDayScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, "Corte");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
                var now = scope.ServiceProvider.GetRequiredService<IClock>().BusinessNow;
                Skip.If(now.Hour >= 22, "No hay margen para una cita futura el mismo dia.");

                var employeeId = (await db.Employees.FirstAsync(e => e.BusinessId == owner.Business.Id)).Id;
                var client = new Client { Name = "Cliente Test", Phone = "600111222", Email = "cli@test.local" };
                db.Clients.Add(client);
                await db.SaveChangesAsync();

                var start = now.AddHours(1);
                db.Appointments.Add(new Appointment
                {
                    ClientId = client.Id,
                    EmployeeId = employeeId,
                    ServiceId = service.Id,
                    StartDate = start,
                    EndDate = start.AddMinutes(30),
                    Status = AppointmentStatus.Confirmed,
                });
                await db.SaveChangesAsync();
            }

            var body = await NotifyDelayAsync(owner, new NotifyDelayDto(null, 20, null));

            Assert.True(body.Notified >= 1);
        }

        // ----- Helpers -----

        private async Task<DelayNotificationResultDto> NotifyDelayAsync(RegisteredOwner owner, NotifyDelayDto dto)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/notify-delay")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<DelayNotificationResultDto>();
            Assert.NotNull(body);
            return body!;
        }

        private async Task GenerateAllDayScheduleAsync(RegisteredOwner owner)
        {
            var year = DateTime.Today.Year;
            var request = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: year,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(year, 1, 1),
                        EffectiveTo: new DateOnly(year, 12, 31),
                        IsDefault: true,
                        WeeklySlots: Enum.GetValues<DayOfWeek>()
                            .Select(d => new CreateWeeklyTimeSlotDto(d, new TimeOnly(1, 0), new TimeOnly(23, 0), TimeSlotType.Regular))
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
