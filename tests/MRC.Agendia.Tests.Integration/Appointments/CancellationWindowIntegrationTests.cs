using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for the self-service cancellation window (issue #171):
    /// a client cannot cancel/reschedule their own appointment once it is inside
    /// the business's advance-notice window, but staff always can, and outside the
    /// window the client can.
    ///
    /// The appointment is far in the future (2035) so a "blocking" window is simply
    /// one large enough to contain it for any plausible test-run date; this keeps
    /// the test deterministic without overriding the clock.
    /// </summary>
    public class CancellationWindowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        // Large enough (~22 years) that the 2035 appointment is inside the window:
        // the client is "too late" to self-cancel.
        private const int BlockingWindowHours = 200_000;
        // Tiny window: the far-future appointment is well outside it -> self-cancel allowed.
        private const int PermissiveWindowHours = 1;

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CancellationWindowIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ClienteBorra_DentroDeLaVentana_DevuelveBadRequest()
        {
            var (owner, clientToken, appointment) = await BookForClientAsync("cw-del-in");
            await SetCancellationWindowAsync(owner.Business.Id, BlockingWindowHours);

            var response = await DeleteAsClientAsync(appointment.Id, clientToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("CANCELLATION_WINDOW_ELAPSED", error!.Code);
        }

        [Fact]
        public async Task ClientePoneCancelled_DentroDeLaVentana_DevuelveBadRequest()
        {
            var (owner, clientToken, appointment) = await BookForClientAsync("cw-put-in");
            await SetCancellationWindowAsync(owner.Business.Id, BlockingWindowHours);

            var update = new UpdateAppointmentDto(
                appointment.Id, appointment.ClientId, appointment.EmployeeId, appointment.ServiceId,
                appointment.StartDate, appointment.EndDate, AppointmentStatus.Cancelled, appointment.Notes);

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Appointment/{appointment.Id}")
            {
                Content = JsonContent.Create(update)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("CANCELLATION_WINDOW_ELAPSED", error!.Code);
        }

        [Fact]
        public async Task ClienteBorra_FueraDeLaVentana_Exito()
        {
            var (owner, clientToken, appointment) = await BookForClientAsync("cw-del-out");
            await SetCancellationWindowAsync(owner.Business.Id, PermissiveWindowHours);

            var response = await DeleteAsClientAsync(appointment.Id, clientToken);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task StaffBorra_DentroDeLaVentana_Exito()
        {
            var (owner, _, appointment) = await BookForClientAsync("cw-staff-in");
            await SetCancellationWindowAsync(owner.Business.Id, BlockingWindowHours);

            // The owner (staff) is never subject to the window.
            var response = await DeleteAsClientAsync(appointment.Id, owner.Token);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        // ----- Flow helpers -----

        private async Task<(ProvisionedOwner Owner, string ClientToken, AppointmentDto Appointment)> BookForClientAsync(string slug)
        {
            var owner = await RegisterOwnerAsync(slug);
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsAsync(owner, "Corte");
            var employeeId = owner.EmployeeId;
            var (_, clientToken, clientId) = await TestProvisioning.ProvisionClientAsync(_client, slug);
            var appointment = await BookAppointmentAsync(owner, clientId, employeeId, service.Id);
            return (owner, clientToken, appointment);
        }

        private async Task<HttpResponseMessage> DeleteAsClientAsync(int appointmentId, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Appointment/{appointmentId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task SetCancellationWindowAsync(int businessId, int? hours)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var business = await db.Businesses.FirstAsync(b => b.Id == businessId);
            business.CancellationWindowHours = hours;
            await db.SaveChangesAsync();
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

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);

        private sealed record ApiError(string Code, string Message);
    }
}
