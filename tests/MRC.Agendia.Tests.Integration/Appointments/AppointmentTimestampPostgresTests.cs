using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;
// The sibling "...Integration.Business" namespace (a test folder) shadows the entity.
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// Verifies against a REAL PostgreSQL (Testcontainers) that an appointment with
    /// WALL-CLOCK dates (DateTime with Kind=Unspecified, as they arrive from JSON and
    /// as BusinessClock.BusinessNow returns them) persists and reads back unchanged.
    ///
    /// Before mapping StartDate/EndDate to `timestamp without time zone`, Npgsql threw
    /// on SaveChanges ("Cannot write DateTime with Kind=Unspecified to PostgreSQL type
    /// 'timestamp with time zone', only UTC is supported"), 500-ing every appointment
    /// create/move on real Postgres. The InMemory suite could not catch it because it
    /// does not validate DateTime.Kind.
    ///
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class AppointmentTimestampPostgresTests
    {
        private readonly PostgresContainerFixture _postgres;

        public AppointmentTimestampPostgresTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [SkippableFact]
        public async Task Appointment_WithWallClockDates_PersistsAndReadsBackUnchanged()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test omitido.");

            await using var db = _postgres.CreateContext();

            var business = new BusinessEntity { IsActive = true, DefaultLanguage = "es" };
            db.Businesses.Add(business);
            var employee = new Employee { BusinessId = business.Id, IsActive = true, MaxConcurrentAppointments = 1 };
            db.Employees.Add(employee);
            var service = new Service { BusinessId = business.Id, DurationMinutes = 30 };
            db.Services.Add(service);
            await db.SaveChangesAsync();

            // Wall-clock start, exactly as it arrives from a JSON body without a zone.
            var start = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Unspecified);
            var appointment = new Appointment
            {
                ClientUserId = "harmony-abc",
                EmployeeId = employee.Id,
                ServiceId = service.Id,
                StartDate = start,
                EndDate = start.AddMinutes(30),
                Status = AppointmentStatus.Pending
            };
            db.Appointments.Add(appointment);

            // Before the fix this threw (timestamptz + Kind=Unspecified). It must persist now.
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var read = await db.Appointments.SingleAsync(a => a.Id == appointment.Id);

            // Same wall-clock value, no timezone shift and no forced UTC Kind.
            Assert.Equal(start, read.StartDate);
            Assert.Equal(start.AddMinutes(30), read.EndDate);
            Assert.Equal(DateTimeKind.Unspecified, read.StartDate.Kind);
        }

        [SkippableFact]
        public async Task Migrations_ApplyCleanly_NoPendingAfterMigrate()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test omitido.");

            // The fixture applies migrations on startup; nothing should remain pending,
            // which also proves the migration chain runs cleanly from scratch.
            await using var db = _postgres.CreateContext();
            var pending = await db.Database.GetPendingMigrationsAsync();
            Assert.Empty(pending);
        }
    }
}
