using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
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
    /// End-to-end coverage for recurring appointment series (issue #174): bulk
    /// create with a real schedule + validator, plus cancel/move/delete by series
    /// id and the Staff-only authorization.
    /// </summary>
    public class RecurringAppointmentSeriesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        // Far enough in the future that every occurrence is "tomorrow or later".
        private const int SeriesYear = 2035;

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RecurringAppointmentSeriesIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateSeries_Weekly_CreaUnaCitaPorSemana_ConMismoSeriesId()
        {
            var setup = await SetupBookableBusinessAsync("rec-create");

            var result = await CreateSeriesAsync(setup);

            Assert.Equal(4, result.Created.Count); // 4 weekly occurrences
            Assert.Empty(result.Skipped);
            Assert.NotEqual(Guid.Empty, result.SeriesId);
            Assert.All(result.Created, c => Assert.Equal(result.SeriesId, c.SeriesId));

            // Persisted with the series id.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Appointments.IgnoreQueryFilters()
                .Where(a => a.SeriesId == result.SeriesId).ToListAsync();
            Assert.Equal(4, stored.Count);
        }

        [Fact]
        public async Task CreateSeries_ComoCliente_DevuelveForbidden()
        {
            var setup = await SetupBookableBusinessAsync("rec-forbidden");
            var clientToken = CreateClientToken("rec-cli");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment/series")
            {
                Content = JsonContent.Create(BuildWeeklySeries(setup))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task CancelSeries_PasaLasFuturasACancelled()
        {
            var setup = await SetupBookableBusinessAsync("rec-cancel");
            var series = await CreateSeriesAsync(setup);

            var body = await SendSeriesAsync<AppointmentSeriesCountResultDto>(
                HttpMethod.Post, $"/api/Appointment/series/{series.SeriesId}/cancel", setup.Token);

            Assert.Equal(4, body.Affected);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Appointments.IgnoreQueryFilters()
                .Where(a => a.SeriesId == series.SeriesId).ToListAsync();
            Assert.All(stored, a => Assert.Equal(AppointmentStatus.Cancelled, a.Status));
        }

        [Fact]
        public async Task MoveSeries_CambiaLaHoraDeLasFuturas()
        {
            var setup = await SetupBookableBusinessAsync("rec-move");
            var series = await CreateSeriesAsync(setup);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"/api/Appointment/series/{series.SeriesId}/move")
            {
                Content = JsonContent.Create(new MoveAppointmentSeriesDto(NewStartTime: new TimeOnly(11, 0), DayShift: 0))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", setup.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<MoveAppointmentSeriesResultDto>();

            Assert.NotNull(body);
            Assert.Equal(4, body!.Moved.Count);
            Assert.Empty(body.Skipped);
            Assert.All(body.Moved, m => Assert.Equal(new TimeOnly(11, 0), TimeOnly.FromDateTime(m.StartDate)));
        }

        [Fact]
        public async Task DeleteSeries_SoftDeleteDeTodaLaSerie()
        {
            var setup = await SetupBookableBusinessAsync("rec-delete");
            var series = await CreateSeriesAsync(setup);

            var body = await SendSeriesAsync<AppointmentSeriesCountResultDto>(
                HttpMethod.Delete, $"/api/Appointment/series/{series.SeriesId}", setup.Token);

            Assert.Equal(4, body.Affected);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Appointments.IgnoreQueryFilters()
                .Where(a => a.SeriesId == series.SeriesId).ToListAsync();
            Assert.All(stored, a => Assert.True(a.IsDeleted));
        }

        [Fact]
        public async Task CancelSeries_SerieInexistente_Devuelve404()
        {
            var setup = await SetupBookableBusinessAsync("rec-404");

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"/api/Appointment/series/{Guid.NewGuid()}/cancel");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", setup.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<TResult> SendSeriesAsync<TResult>(HttpMethod method, string url, string token)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<TResult>();
            Assert.NotNull(body);
            return body!;
        }

        private async Task<AppointmentSeriesResultDto> CreateSeriesAsync(BookableSetup setup)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment/series")
            {
                Content = JsonContent.Create(BuildWeeklySeries(setup))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", setup.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<AppointmentSeriesResultDto>();
            Assert.NotNull(body);
            return body!;
        }

        private static CreateAppointmentSeriesDto BuildWeeklySeries(BookableSetup setup)
        {
            var start = new DateOnly(SeriesYear, 6, 4);
            return new CreateAppointmentSeriesDto(
                ClientId: setup.ClientId,
                EmployeeId: setup.EmployeeId,
                ServiceId: setup.ServiceId,
                StartTime: new TimeOnly(10, 0),
                Frequency: RecurrenceFrequency.Weekly,
                Interval: 1,
                DaysOfWeek: new[] { start.DayOfWeek },
                DayOfMonth: null,
                StartDate: start,
                UntilDate: start.AddDays(21), // 4 occurrences: day 0, 7, 14, 21
                Notes: "Clase semanal");
        }

        private async Task<BookableSetup> SetupBookableBusinessAsync(string slug)
        {
            var owner = await RegisterOwnerAsync(slug);

            // Open all 7 weekdays 9-18 for the series year so any chosen weekday fits.
            var generate = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: SeriesYear,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(SeriesYear, 1, 1),
                        EffectiveTo: new DateOnly(SeriesYear, 12, 31),
                        IsDefault: true,
                        WeeklySlots: Enum.GetValues<DayOfWeek>()
                            .Select(d => new CreateWeeklyTimeSlotDto(d, new TimeOnly(9, 0), new TimeOnly(18, 0), TimeSlotType.Regular))
                            .ToList()),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null);

            using (var gen = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/schedules/generate")
            {
                Content = JsonContent.Create(generate)
            })
            {
                gen.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                (await _client.SendAsync(gen)).EnsureSuccessStatusCode();
            }

            var service = await CreateServiceAsAsync(owner, "Clase");

            var employeeId = owner.EmployeeId;
            int clientId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

                var client = new Client { Name = "Cliente Test", Phone = "600111222" };
                db.Clients.Add(client);
                await db.SaveChangesAsync();
                clientId = client.Id;
            }

            return new BookableSetup(owner.Token, owner.Business.Id, employeeId, clientId, service.Id);
        }

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner, string name)
        {
            var dto = new CreateServiceDto(
                BusinessId: owner.Business.Id,
                Name: name,
                Description: null,
                DurationMinutes: 30,
                Price: 20m);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service")
            {
                Content = JsonContent.Create(dto)
            };
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

        private sealed record BookableSetup(string Token, int BusinessId, int EmployeeId, int ClientId, int ServiceId);
    }
}
