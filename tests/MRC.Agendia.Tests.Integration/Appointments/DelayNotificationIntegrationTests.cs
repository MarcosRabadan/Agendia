using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for the delay alert (issue #168): Staff-only endpoint, and
    /// notifying the upcoming appointments of "today".
    ///
    /// <para>The clock is pinned (#310). The happy path needs "an appointment later today",
    /// which real time cannot promise: the test used to skip itself from 22:00 onwards, so
    /// the only end-to-end cover of this flow quietly disappeared on a nightly CI and the
    /// suite still reported green. The DATE stays today - the generated schedule is for the
    /// current year - and only the time of day is fixed.</para>
    /// </summary>
    public class DelayNotificationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private static readonly DateTime FixedNow = DateTime.Today.AddHours(10);

        private readonly WebApplicationFactory<Program> _host;
        private readonly HttpClient _client;

        public DelayNotificationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _host = factory.WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IClock>();
                    services.AddSingleton<IClock>(new FixedClock(FixedNow));
                }));

            // Everything - client and scopes - goes through _host, so the requests and the
            // seeding share one service provider and therefore one in-memory store.
            _client = _host.CreateClient();
        }

        [Fact]
        public async Task NotifyDelay_ComoCliente_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("delay-forbidden");
            var clientToken = CreateClientToken("delay-cli");

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

        [Fact]
        public async Task NotifyDelay_ConCitaFuturaHoy_NotificaAlMenosUna()
        {
            var owner = await RegisterOwnerAsync("delay-ok");
            await GenerateAllDayScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner);

            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
                var employeeId = owner.EmployeeId;

                // 11:00 against a pinned 10:00: always later today, whatever time it really is.
                var start = FixedNow.AddHours(1);
                db.Appointments.Add(new Appointment
                {
                    ClientUserId = "harmony-delay-test",
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

        private async Task<DelayNotificationResultDto> NotifyDelayAsync(ProvisionedOwner owner, NotifyDelayDto dto)
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

        private async Task GenerateAllDayScheduleAsync(ProvisionedOwner owner)
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

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner)
        {
            var dto = new CreateServiceDto(owner.Business.Id, 30);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        // The 403 check only needs a caller carrying the Client role, so a forged
        // Harmony token is enough: no Client row is involved.
        private static string CreateClientToken(string slug) =>
            TestTokenFactory.Create($"harmony-cli-{slug}-{Guid.NewGuid():N}", Roles.Client);

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
