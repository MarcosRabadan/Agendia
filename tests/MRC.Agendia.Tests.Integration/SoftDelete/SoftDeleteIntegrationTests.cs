using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.SoftDelete
{
    /// <summary>
    /// End-to-end coverage for issue #52: deleting a resource hides it (soft delete)
    /// instead of removing the row, audit fields are filled by the interceptor, and
    /// an Admin can restore a previously deleted resource.
    /// </summary>
    public class SoftDeleteIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public SoftDeleteIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DeleteService_OcultaElServicio_PeroNoBorraLaFila()
        {
            var owner = await RegisterOwnerAsync("sd-del");
            var service = await CreateServiceAsAsync(owner);

            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Service/{service.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                var delResponse = await _client.SendAsync(del);
                Assert.Equal(HttpStatusCode.NoContent, delResponse.StatusCode);
            }

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            // Hidden by the global query filter, but still physically present.
            Assert.False(await db.Services.AnyAsync(s => s.Id == service.Id));
            var stored = await db.Services.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == service.Id);
            Assert.NotNull(stored);
            Assert.True(stored!.IsDeleted);
            Assert.NotNull(stored.DeletedAt);
        }

        [Fact]
        public async Task RestoreService_ComoAdmin_RecuperaElServicio()
        {
            var owner = await RegisterOwnerAsync("sd-restore");
            var service = await CreateServiceAsAsync(owner);

            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Service/{service.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
                (await _client.SendAsync(del)).EnsureSuccessStatusCode();
            }

            var adminToken = NewAdminToken();
            using (var restore = new HttpRequestMessage(HttpMethod.Post, $"/api/Service/{service.Id}/restore"))
            {
                restore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                var restoreResponse = await _client.SendAsync(restore);
                Assert.Equal(HttpStatusCode.NoContent, restoreResponse.StatusCode);
            }

            // Live again after restore: the global query filter no longer hides it.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FirstOrDefaultAsync(s => s.Id == service.Id);
            Assert.NotNull(stored);
            Assert.False(stored!.IsDeleted);
        }

        [Fact]
        public async Task RestoreService_ComoOwner_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("sd-forbidden");
            var service = await CreateServiceAsAsync(owner);

            using var restore = new HttpRequestMessage(HttpMethod.Post, $"/api/Service/{service.Id}/restore");
            restore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(restore);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Availability_DeNegocioBorrado_Devuelve404()
        {
            var owner = await RegisterOwnerAsync("sd-avail");
            var service = await CreateServiceAsAsync(owner);

            var adminToken = NewAdminToken();
            using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/Business/{owner.Business.Id}"))
            {
                del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                (await _client.SendAsync(del)).EnsureSuccessStatusCode();
            }

            var date = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");
            var get = await _client.GetAsync(
                $"/api/businesses/{owner.Business.Id}/availability?date={date}&serviceId={service.Id}");

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact]
        public async Task CreateService_RellenaAuditFields()
        {
            var owner = await RegisterOwnerAsync("sd-audit");
            var service = await CreateServiceAsAsync(owner);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Services.FindAsync(service.Id);

            Assert.NotNull(stored);
            Assert.NotEqual(default, stored!.CreatedAt);
            Assert.False(string.IsNullOrEmpty(stored.CreatedBy));
            Assert.Null(stored.UpdatedAt);
        }

        /// <summary>
        /// A live appointment survives its employee being soft-deleted (#292). Resource
        /// authorization reached the owning business through the required
        /// <c>Appointment.Employee</c> navigation, whose soft-delete filter turned the
        /// projection into an INNER JOIN that DROPPED the appointment: the client of a real,
        /// future booking got a 404 for their own appointment as soon as the employee left the
        /// business, and so did the owner. The appointment keeps its history, so it must stay
        /// readable and cancellable.
        /// </summary>
        [Theory]
        [InlineData(true)]  // the client of the appointment
        [InlineData(false)] // the owner of the business
        public async Task Appointment_ConEmpleadoBorrado_SigueAccesibleParaQuienLeCorresponde(bool asClient)
        {
            var (setup, clientToken, appointment) = await BookWithClientAsync("sd-appt-emp");
            await DeleteEmployeeAsync(setup);

            var token = asClient ? clientToken : setup.OwnerToken;

            var get = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Get, $"/api/Appointment/{appointment.Id}", token);

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            // #332: the acceptance criteria of #292 said "readable AND cancellable", but only the
            // GET became an assert. The PUT authorizes twice - the appointment and the
            // destination - and the second lookup kept its filters, so cancelling by PUT was a
            // 404 while cancelling by DELETE worked. Same act, different verb, different answer.
            var put = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Put, $"/api/Appointment/{appointment.Id}", token,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: appointment.ClientUserId,
                    EmployeeId: appointment.EmployeeId,
                    ServiceId: appointment.ServiceId,
                    StartDate: appointment.StartDate,
                    EndDate: appointment.EndDate,
                    Status: AppointmentStatus.Cancelled,
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            Assert.Equal(AppointmentStatus.Cancelled, await ReadStatusAsync(appointment.Id));
        }

        /// <summary>
        /// The consequence that could not be undone: a class that already happened, whose teacher
        /// then left the academy, could never be closed as Completed or NoShow. Those bookings
        /// stayed Pending/Confirmed for good and kept polluting the business statistics and every
        /// student's reliability index, with no way out short of an UPDATE by hand (#332).
        /// </summary>
        [Theory]
        [InlineData(AppointmentStatus.Completed)]
        [InlineData(AppointmentStatus.NoShow)]
        public async Task AppointmentPasada_ConEmpleadoBorrado_SePuedeCerrar(AppointmentStatus status)
        {
            var (setup, _, appointment) = await BookWithClientAsync($"sd-past-{status}");
            await DeleteEmployeeAsync(setup);
            var (start, end) = await MoveToThePastAsync(appointment.Id);

            var put = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Put, $"/api/Appointment/{appointment.Id}", setup.OwnerToken,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: appointment.ClientUserId,
                    EmployeeId: appointment.EmployeeId,
                    ServiceId: appointment.ServiceId,
                    StartDate: start,
                    EndDate: end,
                    Status: status,
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            Assert.Equal(status, await ReadStatusAsync(appointment.Id));
        }

        /// <summary>
        /// Regression, and the reason the fix is safe: authorization no longer rejects a
        /// soft-deleted employee, but BOOKING one still fails with the very same 404 - the
        /// scheduling validator reads the employee through the filtered repository. Only the
        /// layer that answers changes; from outside it is indistinguishable.
        /// </summary>
        [Fact]
        public async Task CrearCita_SobreEmpleadoBorrado_Sigue404()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "sd-book-gone", SeriesYear);
            await DeleteEmployeeAsync(setup);

            var start = SeriesDay.ToDateTime(new TimeOnly(12, 0));
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                    start, start.AddMinutes(30), null));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("EMPLOYEE_NOT_FOUND", error!.Code);
        }

        /// <summary>
        /// And the series takes the whole request down with it, rather than reporting one skip per
        /// occurrence: a missing employee is a request-level failure (`IsRequestLevel`).
        /// </summary>
        [Fact]
        public async Task CrearSerie_SobreEmpleadoBorrado_Sigue404()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "sd-series-gone", SeriesYear);
            await DeleteEmployeeAsync(setup);

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                "/api/Appointment/series", setup.OwnerToken,
                new CreateAppointmentSeriesDto(
                    ClientUserId: setup.ClientUserId,
                    EmployeeId: setup.EmployeeId,
                    ServiceId: setup.Service.Id,
                    StartTime: new TimeOnly(13, 0),
                    Frequency: RecurrenceFrequency.Weekly,
                    Interval: 1,
                    DaysOfWeek: new[] { SeriesDay.DayOfWeek },
                    DayOfMonth: null,
                    StartDate: SeriesDay,
                    UntilDate: SeriesDay.AddDays(7),
                    Notes: null));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("EMPLOYEE_NOT_FOUND", error!.Code);
        }

        /// <summary>
        /// The same for the series endpoints, which resolve the owning business the same way.
        /// </summary>
        [Fact]
        public async Task Serie_ConEmpleadoBorrado_SigueGestionablePorElNegocio()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "sd-series-emp", SeriesYear);

            var seriesResponse = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                "/api/Appointment/series", setup.OwnerToken,
                new CreateAppointmentSeriesDto(
                    ClientUserId: setup.ClientUserId,
                    EmployeeId: setup.EmployeeId,
                    ServiceId: setup.Service.Id,
                    StartTime: new TimeOnly(11, 0),
                    Frequency: RecurrenceFrequency.Weekly,
                    Interval: 1,
                    DaysOfWeek: new[] { SeriesDay.DayOfWeek },
                    DayOfMonth: null,
                    StartDate: SeriesDay,
                    UntilDate: SeriesDay.AddDays(7),
                    Notes: null));
            seriesResponse.EnsureSuccessStatusCode();
            var series = await seriesResponse.Content.ReadFromJsonAsync<AppointmentSeriesResultDto>();
            Assert.Equal(2, series!.Created.Count);

            await DeleteEmployeeAsync(setup);

            var cancel = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                $"/api/Appointment/series/{series.SeriesId}/cancel", setup.OwnerToken);

            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        }

        /// <summary>
        /// Control for the fix above: dropping the query filters must NOT start telling a
        /// stranger that someone else's appointment exists. Cross-tenant stays a 404 (the R7
        /// convention), not a 403.
        /// </summary>
        [Fact]
        public async Task Appointment_DeOtroNegocio_SigueSiendo404ParaUnExtrano()
        {
            var (setup, _, appointment) = await BookWithClientAsync("sd-appt-tenant");
            var stranger = await TestProvisioning.ProvisionOwnerAsync(_client, "sd-appt-stranger");

            var get = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Get, $"/api/Appointment/{appointment.Id}", stranger.Token);

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        /// <summary>And a soft-deleted appointment stays hidden, employee alive or not.</summary>
        [Fact]
        public async Task Appointment_Borrada_SigueDevolviendo404()
        {
            var (setup, clientToken, appointment) = await BookWithClientAsync("sd-appt-gone");

            var delete = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Delete, $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            delete.EnsureSuccessStatusCode();

            var get = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Get, $"/api/Appointment/{appointment.Id}", clientToken);

            Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        /// <summary>
        /// Restoring a future appointment cannot overbook (#294): while it was deleted the slot
        /// was free, so someone else may well have taken it. The employee's capacity is 1 here,
        /// so bringing the first one back would put two live appointments on one seat.
        /// </summary>
        [Fact]
        public async Task RestoreAppointment_ConLaFranjaYaOcupada_DevuelveConflicto()
        {
            var (setup, _, appointment) = await BookWithClientAsync("sd-restore-full");

            var delete = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Delete, $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            delete.EnsureSuccessStatusCode();

            // Another client takes the freed slot with the same employee.
            var taken = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(BookableBusinessFactory.CounterClientUserId(), setup.EmployeeId,
                    setup.Service.Id, appointment.StartDate, appointment.EndDate, null));
            Assert.Equal(HttpStatusCode.Created, taken.StatusCode);

            var restore = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Post, $"/api/Appointment/{appointment.Id}/restore", NewAdminToken());

            Assert.Equal(HttpStatusCode.BadRequest, restore.StatusCode);
            var error = await restore.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_CONFLICT", error!.Code);

            // Still deleted: the rejection left nothing half-applied.
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Appointments.IgnoreQueryFilters().FirstAsync(a => a.Id == appointment.Id);
            Assert.True(stored.IsDeleted);
        }

        /// <summary>Positive control: with the slot still free the restore goes through.</summary>
        [Fact]
        public async Task RestoreAppointment_ConLaFranjaLibre_Restaura()
        {
            var (setup, _, appointment) = await BookWithClientAsync("sd-restore-free");

            var delete = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Delete, $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            delete.EnsureSuccessStatusCode();

            var restore = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Post, $"/api/Appointment/{appointment.Id}/restore", NewAdminToken());

            Assert.Equal(HttpStatusCode.NoContent, restore.StatusCode);
            var alive = await BookableBusinessFactory.GetAppointmentAsync(_client, setup.OwnerToken, appointment.Id);
            Assert.Equal(appointment.Id, alive.Id);
        }

        // ----- Helpers -----

        private const int SeriesYear = 2036;
        private static readonly DateOnly SeriesDay = new(SeriesYear, 6, 3);

        /// <summary>A bookable business with one future appointment booked for a real Client token.</summary>
        private async Task<(BookableBusiness Setup, string ClientToken, AppointmentDto Appointment)> BookWithClientAsync(string slug)
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, slug, SeriesYear);
            var clientToken = TestTokenFactory.Create(setup.ClientUserId, Roles.Client);

            var start = SeriesDay.ToDateTime(new TimeOnly(10, 0));
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                    start, start.AddMinutes(30), null));
            response.EnsureSuccessStatusCode();

            return (setup, clientToken, (await response.Content.ReadFromJsonAsync<AppointmentDto>())!);
        }

        /// <summary>
        /// Drags the appointment into the past, as time passing would, and returns its new wall
        /// clock so the caller can send it back unchanged: a PUT that also moved the dates would
        /// be re-validated and rejected for being in the past.
        /// </summary>
        private async Task<(DateTime Start, DateTime End)> MoveToThePastAsync(Guid appointmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            // IgnoreQueryFilters: with the employee soft-deleted the required navigation would
            // drop the row here too - the very defect under test, one layer down.
            var appointment = await db.Appointments.IgnoreQueryFilters().FirstAsync(a => a.Id == appointmentId);
            appointment.StartDate = new DateTime(2020, 3, 2, 10, 0, 0);
            appointment.EndDate = appointment.StartDate.AddMinutes(30);
            await db.SaveChangesAsync();
            return (appointment.StartDate, appointment.EndDate);
        }

        private async Task<AppointmentStatus> ReadStatusAsync(Guid appointmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var stored = await db.Appointments.AsNoTracking().IgnoreQueryFilters()
                .FirstAsync(a => a.Id == appointmentId);
            return stored.Status;
        }

        private async Task DeleteEmployeeAsync(BookableBusiness setup)
        {
            var response = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Delete, $"/api/Employee/{setup.EmployeeId}", setup.OwnerToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task<ServiceDto> CreateServiceAsAsync(ProvisionedOwner owner)
        {
            var dto = new CreateServiceDto(owner.Business.Id, DurationMinutes: 30);

            return await TestProvisioning.PostAsync<CreateServiceDto, ServiceDto>(
                _client, "/api/Service", dto, owner.Token);
        }

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);

        // Agendia no longer stores users, so an Admin is just a forged Harmony token.
        private static string NewAdminToken() =>
            TestTokenFactory.Create($"harmony-admin-{Guid.NewGuid():N}", Roles.Admin);
    }
}
