using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// What the integration events actually CARRY, read back from the outbox row the request
    /// wrote (#293). The unit tests cover which event is raised, but they stub
    /// <c>GetNotificationContextAsync</c> with a fixed context, so they cannot see whether the
    /// payload matches the appointment's real state after the update. That needs the real
    /// repository and the real DbContext, which is what this class exercises.
    ///
    /// InMemory is enough: nothing here depends on PostgreSQL semantics, only on EF not
    /// flushing pending changes before a query.
    /// </summary>
    public class AppointmentEventPayloadIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2037;
        private static readonly DateOnly Day = new(Year, 6, 2);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AppointmentEventPayloadIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        /// <summary>
        /// Moving an appointment to a different employee AND a different time must announce the
        /// DESTINATION employee. The reschedule notification is what tells the client where to
        /// turn up, so naming the employee they no longer have is worse than saying nothing.
        /// </summary>
        [Fact]
        public async Task Rescheduling_to_another_employee_reports_the_destination_employee()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-move", Year);
            var colleague = await CreateColleagueAsync(setup);
            var appointment = await BookAsync(setup, new TimeOnly(10, 0));

            var newStart = Day.ToDateTime(new TimeOnly(12, 0));
            var update = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: appointment.ClientUserId,
                    EmployeeId: colleague,          // moved to the colleague
                    ServiceId: appointment.ServiceId,
                    StartDate: newStart,
                    EndDate: newStart.AddMinutes(30),
                    Status: appointment.Status,
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var payload = await ReadEventAsync(appointment.Id, "AppointmentRescheduled");

            // The new dates were already right (they are passed from the tracked entity).
            Assert.Equal(newStart, payload.GetProperty("startDate").GetDateTime());
            Assert.Equal(appointment.StartDate, payload.GetProperty("previousStartDate").GetDateTime());

            // The employee came from a database read issued BEFORE the save, so it used to be
            // the employee the appointment was moved AWAY from.
            Assert.Equal(colleague, payload.GetProperty("employeeId").GetGuid());
        }

        /// <summary>
        /// Control: a plain time change keeps the same employee, so the fix cannot be "report
        /// whatever the request said" without noticing.
        /// </summary>
        [Fact]
        public async Task Rescheduling_only_the_time_keeps_the_same_employee()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-time", Year);
            var appointment = await BookAsync(setup, new TimeOnly(10, 0));

            var newStart = Day.ToDateTime(new TimeOnly(15, 0));
            var update = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: appointment.ClientUserId,
                    EmployeeId: appointment.EmployeeId,
                    ServiceId: appointment.ServiceId,
                    StartDate: newStart,
                    EndDate: newStart.AddMinutes(30),
                    Status: appointment.Status,
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var payload = await ReadEventAsync(appointment.Id, "AppointmentRescheduled");
            Assert.Equal(setup.EmployeeId, payload.GetProperty("employeeId").GetGuid());
            Assert.Equal(newStart, payload.GetProperty("startDate").GetDateTime());
        }

        /// <summary>
        /// Cancelling by DELETE announces the cancellation just like cancelling by PUT (#296).
        /// Before this, the same act notified or not depending on the verb the front used, so a
        /// student who dropped the class could vanish from the academy's agenda in silence.
        /// </summary>
        [Fact]
        public async Task Deleting_a_live_appointment_announces_the_cancellation()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-del", Year);
            var appointment = await BookAsync(setup, new TimeOnly(16, 0));

            var delete = await BookableBusinessFactory.SendAsync(
                _client, HttpMethod.Delete, $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var payload = await ReadEventAsync(appointment.Id, "AppointmentCancelled");
            Assert.Equal(appointment.ClientUserId, payload.GetProperty("clientUserId").GetString());
            Assert.Equal(appointment.StartDate, payload.GetProperty("startDate").GetDateTime());
        }

        // ----- Helpers -----

        private async Task<AppointmentDto> BookAsync(BookableBusiness setup, TimeOnly at)
        {
            var start = Day.ToDateTime(at);
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                    start, start.AddMinutes(30), null));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
        }

        private async Task<Guid> CreateColleagueAsync(BookableBusiness setup)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Employee",
                setup.OwnerToken, new CreateEmployeeDto(BusinessId: setup.BusinessId, UserId: null));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!.Id;
        }

        /// <summary>The single outbox payload of that type written for this appointment.</summary>
        private async Task<JsonElement> ReadEventAsync(Guid appointmentId, string type)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var payloads = await db.OutboxMessages
                .Where(m => m.Type == type)
                .Select(m => m.Payload)
                .ToListAsync();

            // Cloned: the JsonDocument is disposed as soon as it goes out of scope.
            var matching = payloads
                .Select(p => JsonDocument.Parse(p).RootElement.Clone())
                .Where(e => e.GetProperty("appointmentId").GetGuid() == appointmentId)
                .ToList();

            return Assert.Single(matching);
        }
    }
}
