using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for multiservice appointments (issue #170): a booking
    /// can bundle extra services; the total duration is validated and the extras
    /// are persisted and echoed back, and availability sizes slots by the total.
    /// </summary>
    public class MultiserviceAppointmentIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MultiserviceAppointmentIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CrearCitaMultiservicio_DuracionTotalCorrecta_PersisteYDevuelveLosExtras()
        {
            var owner = await RegisterOwnerAsync("ms-ok");
            await GenerateScheduleAsync(owner);
            var primary = await CreateServiceAsync(owner, "Corte", durationMinutes: 30, price: 20m);
            var extra = await CreateServiceAsync(owner, "Barba", durationMinutes: 30, price: 12m);
            var (employeeId, clientId) = await SeedEmployeeAndClientAsync(owner);

            var start = SlotDate.ToDateTime(SlotTime);
            var dto = new CreateAppointmentDto(
                clientId, employeeId, primary.Id, start, start.AddMinutes(60), Notes: null,
                ExtraServiceIds: new[] { extra.Id });

            var response = await PostAppointmentAsync(owner.Token, dto);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(created);
            Assert.Equal(primary.Id, created!.ServiceId);
            Assert.Equal(new[] { extra.Id }, created.ExtraServiceIds);

            // The GET echoes the extras too (read path loads them).
            var fetched = await GetAppointmentAsync(owner.Token, created.Id);
            Assert.Equal(new[] { extra.Id }, fetched.ExtraServiceIds);
        }

        [Fact]
        public async Task CrearCitaMultiservicio_DuracionSoloDelPrincipal_DevuelveBadRequest()
        {
            var owner = await RegisterOwnerAsync("ms-bad");
            await GenerateScheduleAsync(owner);
            var primary = await CreateServiceAsync(owner, "Corte", durationMinutes: 30, price: 20m);
            var extra = await CreateServiceAsync(owner, "Barba", durationMinutes: 30, price: 12m);
            var (employeeId, clientId) = await SeedEmployeeAndClientAsync(owner);

            var start = SlotDate.ToDateTime(SlotTime);
            // Only 30 min booked for two 30-min services -> total duration mismatch.
            var dto = new CreateAppointmentDto(
                clientId, employeeId, primary.Id, start, start.AddMinutes(30), Notes: null,
                ExtraServiceIds: new[] { extra.Id });

            var response = await PostAppointmentAsync(owner.Token, dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_DURATION_MISMATCH", error!.Code);
        }

        [Fact]
        public async Task Disponibilidad_ConServiciosExtra_DimensionaPorLaDuracionTotal()
        {
            var owner = await RegisterOwnerAsync("ms-avail");
            await GenerateScheduleAsync(owner);
            var primary = await CreateServiceAsync(owner, "Corte", durationMinutes: 30, price: 20m);
            var extra = await CreateServiceAsync(owner, "Barba", durationMinutes: 30, price: 12m);

            var url = $"/api/businesses/{owner.Business.Id}/availability"
                + $"?date={SlotDate:yyyy-MM-dd}&serviceId={primary.Id}&stepMinutes=30&extraServiceIds={extra.Id}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var availability = await response.Content.ReadFromJsonAsync<AvailabilityDto>();
            Assert.NotNull(availability);
            Assert.Equal(60, availability!.DurationMinutes); // 30 + 30
            Assert.NotEmpty(availability.Slots);
            Assert.All(availability.Slots, s => Assert.Equal(60, (s.EndTime - s.StartTime).TotalMinutes));
        }

        [Fact]
        public async Task ReprogramarCitaMultiservicio_LaRespuestaPutConservaLosExtras()
        {
            var owner = await RegisterOwnerAsync("ms-put");
            await GenerateScheduleAsync(owner);
            var primary = await CreateServiceAsync(owner, "Corte", durationMinutes: 30, price: 20m);
            var extra = await CreateServiceAsync(owner, "Barba", durationMinutes: 30, price: 12m);
            var (employeeId, clientId) = await SeedEmployeeAndClientAsync(owner);

            var start = SlotDate.ToDateTime(SlotTime);
            var createResponse = await PostAppointmentAsync(owner.Token, new CreateAppointmentDto(
                clientId, employeeId, primary.Id, start, start.AddMinutes(60), Notes: null,
                ExtraServiceIds: new[] { extra.Id }));
            createResponse.EnsureSuccessStatusCode();
            var created = await createResponse.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(created);

            // Reschedule to a later 60-min block; the PUT response must still echo the extras.
            var newStart = SlotDate.ToDateTime(new TimeOnly(14, 0));
            var update = new UpdateAppointmentDto(
                created!.Id, clientId, employeeId, primary.Id, newStart, newStart.AddMinutes(60), created.Status, Notes: null);
            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Appointment/{created.Id}")
            {
                Content = JsonContent.Create(update)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(updated);
            Assert.Equal(new[] { extra.Id }, updated!.ExtraServiceIds);
        }

        [Fact]
        public async Task CrearCitaMultiservicio_ConExtrasDuplicados_DevuelveBadRequest()
        {
            var owner = await RegisterOwnerAsync("ms-dup");
            await GenerateScheduleAsync(owner);
            var primary = await CreateServiceAsync(owner, "Corte", durationMinutes: 30, price: 20m);
            var extra = await CreateServiceAsync(owner, "Barba", durationMinutes: 30, price: 12m);
            var (employeeId, clientId) = await SeedEmployeeAndClientAsync(owner);

            var start = SlotDate.ToDateTime(SlotTime);
            var dto = new CreateAppointmentDto(
                clientId, employeeId, primary.Id, start, start.AddMinutes(90), Notes: null,
                ExtraServiceIds: new[] { extra.Id, extra.Id });

            var response = await PostAppointmentAsync(owner.Token, dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<HttpResponseMessage> PostAppointmentAsync(string token, CreateAppointmentDto dto)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<AppointmentDto> GetAppointmentAsync(string token, int id)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Appointment/{id}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        private async Task<(int EmployeeId, int ClientId)> SeedEmployeeAndClientAsync(ProvisionedOwner owner)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var employeeId = owner.EmployeeId;
            var client = new Client { Name = "Cliente MS", Phone = "600111222", Email = $"ms-{Guid.NewGuid():N}@test.local" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return (employeeId, client.Id);
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

        private async Task<ServiceDto> CreateServiceAsync(ProvisionedOwner owner, string name, int durationMinutes, decimal price)
        {
            var dto = new CreateServiceDto(owner.Business.Id, name, null, durationMinutes, price);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);

        private sealed record ApiError(string Code, string Message);
    }
}
