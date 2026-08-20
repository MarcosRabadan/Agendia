using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Tests.Integration.Infrastructure;
// The sibling "...Integration.Business" namespace (a test folder) shadows the entity.
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Integration.Notifications
{
    /// <summary>
    /// Verifies against a REAL PostgreSQL (Testcontainers) that the reminder job is
    /// N-instance safe: two <see cref="ReminderProcessor"/> instances (each with its own
    /// context/connection, mimicking two replicas) running at the same time publish each
    /// reminder EXACTLY once. The pg_try_advisory_lock mutual exclusion is what makes that
    /// hold; the in-memory suite cannot exercise it.
    ///
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class ReminderProcessorConcurrencyPostgresTests
    {
        private readonly PostgresContainerFixture _postgres;

        public ReminderProcessorConcurrencyPostgresTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [SkippableFact]
        public async Task ProcessDueAsync_ConcurrentInstances_PublishEachReminderOnce()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres not available; concurrency test skipped.");

            var now = new DateTime(2030, 6, 1, 10, 0, 0, DateTimeKind.Unspecified);
            var appointmentIds = new List<Guid>();
            await using (var seed = _postgres.CreateContext())
            {
                var business = new BusinessEntity { IsActive = true, DefaultLanguage = "es" };
                var employee = new Employee { Business = business, IsActive = true, MaxConcurrentAppointments = 1 };
                var service = new Service { Business = business, DurationMinutes = 30 };
                seed.AddRange(business, employee, service);
                await seed.SaveChangesAsync();

                for (var i = 0; i < 3; i++)
                {
                    var appointment = new Appointment
                    {
                        ClientUserId = $"harmony-{Guid.NewGuid():N}",
                        EmployeeId = employee.Id,
                        ServiceId = service.Id,
                        StartDate = now.AddHours(2).AddMinutes(i),
                        EndDate = now.AddHours(2).AddMinutes(30 + i),
                        Status = AppointmentStatus.Confirmed
                    };
                    seed.Appointments.Add(appointment);
                    appointmentIds.Add(appointment.Id);
                }
                await seed.SaveChangesAsync();
            }

            var options = Options.Create(new ReminderOptions { ReminderWindowHours = 24 });

            async Task RunInstance()
            {
                await using var ctx = _postgres.CreateContext();
                var processor = new ReminderProcessor(
                    ctx, new FixedClock(now), options, NullLogger<ReminderProcessor>.Instance);
                await processor.ProcessDueAsync();
            }

            // Two instances run the reminder batch at the same time.
            await Task.WhenAll(RunInstance(), RunInstance());

            await using var check = _postgres.CreateContext();

            // Exactly one reminder event enlisted per appointment across both instances: the
            // advisory lock stops the second instance re-publishing.
            foreach (var id in appointmentIds)
            {
                var reminders = await check.Set<OutboxMessage>()
                    .CountAsync(m => m.Type == nameof(AppointmentReminder) && m.Payload.Contains(id.ToString()));
                Assert.Equal(1, reminders);
            }

            var reminded = await check.Appointments
                .IgnoreQueryFilters()
                .CountAsync(a => appointmentIds.Contains(a.Id) && a.ReminderSentAt != null);
            Assert.Equal(3, reminded);
        }
    }
}
