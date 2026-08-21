using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// What the integration events actually CARRY, read back from the outbox row the request
    /// wrote (#293). The unit tests cover which event is raised, but they stub
    /// <c>GetNotificationBusinessByEmployeeAsync</c> with a fixed business, so they cannot see
    /// whether the payload matches the appointment's real state after the update. That needs
    /// the real repository and the real DbContext, which is what this class exercises.
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

        /// <summary>
        /// The payload has to say WHICH zone its wall-clock dates are in (#321). They are
        /// serialized with no zone marker at all, so a consumer that defaulted to UTC - or to
        /// its own local zone - would announce the class at the wrong hour, and could not
        /// correct it because Agendia never told it the zone.
        /// </summary>
        [Fact]
        public async Task The_payload_says_which_zone_its_wall_clock_dates_are_in()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-tz", Year);
            var appointment = await BookAsync(setup, new TimeOnly(11, 0));

            var payload = await ReadEventAsync(appointment.Id, "AppointmentConfirmed");

            Assert.Equal("Europe/Madrid", payload.GetProperty("timeZone").GetString());

            // And the two kinds of instant stay distinguishable: the wall-clock dates carry no
            // zone suffix, while occurredOnUtc is a real UTC instant and keeps its Z.
            var startDate = payload.GetProperty("startDate").GetString();
            Assert.NotNull(startDate);
            Assert.DoesNotContain("Z", startDate!, StringComparison.Ordinal);
            Assert.DoesNotContain("+", startDate!, StringComparison.Ordinal);
            Assert.StartsWith($"{Year}-06-02T11:00:00", startDate!, StringComparison.Ordinal);

            Assert.EndsWith("Z", payload.GetProperty("occurredOnUtc").GetString()!, StringComparison.Ordinal);
        }

        /// <summary>
        /// A PUT that cancels AND hands the class over in the same request. The cancellation is
        /// for whoever HELD it: it used to be addressed with the clientUserId the body carried,
        /// so the notice reached a student who never had the class while the one who lost it was
        /// told nothing (#342).
        /// </summary>
        [Fact]
        public async Task Cancelling_and_reassigning_at_once_names_the_previous_holder()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-cancel-swap", Year);
            var appointment = await BookAsync(setup, new TimeOnly(9, 30));
            var newHolder = BookableBusinessFactory.CounterClientUserId();

            var update = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: newHolder,                // handed over ...
                    EmployeeId: appointment.EmployeeId,
                    ServiceId: appointment.ServiceId,
                    StartDate: appointment.StartDate,
                    EndDate: appointment.EndDate,
                    Status: AppointmentStatus.Cancelled,    // ... and cancelled in the same act
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var payload = await ReadEventAsync(appointment.Id, "AppointmentCancelled");
            Assert.Equal(appointment.ClientUserId, payload.GetProperty("clientUserId").GetString());

            // And nothing is confirmed to the incoming student: the only confirmation for this
            // appointment is still the one the booking wrote.
            Assert.Equal(1, await CountEventsAsync(appointment.Id, "AppointmentConfirmed"));
        }

        /// <summary>
        /// Handing the class over inside the last 24 hours, without moving it: the reminder that
        /// already went out named the student who left, so the new one needs their own. The job
        /// marks ReminderSentAt per appointment, so unless the handover clears it the incoming
        /// student silently inherits "already reminded" and hears nothing before the class (#337).
        /// The job is driven here with a pinned clock, which is what makes the whole chain -
        /// update, mark, job, payload - observable end to end.
        /// </summary>
        [Fact]
        public async Task Handing_the_class_over_re_arms_the_reminder_for_the_new_holder()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "evt-rearm", Year);
            // A day of its own so the job cannot pick up the appointments of the sibling tests.
            var appointment = await BookAsync(setup, new TimeOnly(14, 0), Day.AddDays(1));
            await MarkAsAlreadyRemindedAsync(appointment.Id);

            var newHolder = BookableBusinessFactory.CounterClientUserId();
            var update = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken,
                new UpdateAppointmentDto(
                    Id: appointment.Id,
                    ClientUserId: newHolder,
                    EmployeeId: appointment.EmployeeId,
                    ServiceId: appointment.ServiceId,
                    StartDate: appointment.StartDate,   // same time on purpose: only the holder changes
                    EndDate: appointment.EndDate,
                    Status: appointment.Status,
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var processor = new ReminderProcessor(
                db,
                new FixedClock(appointment.StartDate.AddHours(-2)),
                Options.Create(new ReminderOptions { ReminderWindowHours = 24 }),
                NullLogger<ReminderProcessor>.Instance);

            Assert.Equal(1, await processor.ProcessDueAsync());

            var payload = await ReadEventAsync(appointment.Id, "AppointmentReminder");
            Assert.Equal(newHolder, payload.GetProperty("clientUserId").GetString());
        }

        // ----- Helpers -----

        private async Task<AppointmentDto> BookAsync(BookableBusiness setup, TimeOnly at, DateOnly? on = null)
        {
            var start = (on ?? Day).ToDateTime(at);
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

        /// <summary>Marks the appointment as already reminded, as the 24h job would have.</summary>
        private async Task MarkAsAlreadyRemindedAsync(Guid appointmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var appointment = await db.Appointments.FirstAsync(a => a.Id == appointmentId);
            appointment.ReminderSentAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        /// <summary>The single outbox payload of that type written for this appointment.</summary>
        private async Task<JsonElement> ReadEventAsync(Guid appointmentId, string type) =>
            Assert.Single(await ReadEventsAsync(appointmentId, type));

        /// <summary>How many outbox payloads of that type were written for this appointment.</summary>
        private async Task<int> CountEventsAsync(Guid appointmentId, string type) =>
            (await ReadEventsAsync(appointmentId, type)).Count;

        private async Task<List<JsonElement>> ReadEventsAsync(Guid appointmentId, string type)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var payloads = await db.OutboxMessages
                .Where(m => m.Type == type)
                .Select(m => m.Payload)
                .ToListAsync();

            // Cloned: the JsonDocument is disposed as soon as it goes out of scope.
            return payloads
                .Select(p => JsonDocument.Parse(p).RootElement.Clone())
                .Where(e => e.GetProperty("appointmentId").GetGuid() == appointmentId)
                .ToList();
        }
    }
}
