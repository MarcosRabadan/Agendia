using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;
// The sibling "...Integration.Business" namespace (a test folder) shadows the entity.
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Integration.Database
{
    /// <summary>
    /// Verifies the unique/filtered indexes against a REAL PostgreSQL (Testcontainers).
    /// EF InMemory does not enforce indexes/constraints, so a SQL-only bug (e.g. the
    /// schedule-override unique index that produced a 500 in #188) passes unnoticed under
    /// the green InMemory suite. The fixture builds the full model schema - indexes
    /// included - so these duplicate inserts must be rejected.
    ///
    /// Skipped automatically when Docker is unavailable, mirroring
    /// <c>BookingConcurrencyGuardPostgresTests</c>.
    /// </summary>
    [Collection(PostgresCollection.Name)]
    public class DatabaseConstraintsPostgresTests
    {
        private readonly PostgresContainerFixture _postgres;

        public DatabaseConstraintsPostgresTests(PostgresContainerFixture postgres)
        {
            _postgres = postgres;
        }

        [SkippableFact]
        public async Task ScheduleOverride_DuplicateBusinessDate_IsRejected()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test de constraints omitido.");

            await using var db = _postgres.CreateContext();
            var business = await SeedBusinessAsync(db);
            var date = new DateOnly(2026, 12, 25);

            db.ScheduleOverrides.Add(NewOverride(business.Id, date, "Navidad"));
            await db.SaveChangesAsync();

            // IX_ScheduleOverride_BusinessId_Date is unique: a second override for the
            // same (business, date) violates it. This is the #188 scenario that the
            // InMemory suite could not catch.
            db.ScheduleOverrides.Add(NewOverride(business.Id, date, "Duplicado"));
            await AssertUniqueViolationAsync(() => db.SaveChangesAsync());
        }

        [SkippableFact]
        public async Task WaitlistEntry_DuplicateActiveSlot_IsRejected_ButReJoinAfterCancelIsAllowed()
        {
            Skip.IfNot(_postgres.Available, "Docker/Postgres no disponible; test de constraints omitido.");

            await using var db = _postgres.CreateContext();
            var business = await SeedBusinessAsync(db);
            var service = await SeedServiceAsync(db, business.Id);
            var clientUserId = $"harmony-{Guid.NewGuid():N}";
            var date = new DateOnly(2026, 6, 7);
            var start = new TimeOnly(16, 0);

            WaitlistEntry Entry(WaitlistStatus status) => new()
            {
                BusinessId = business.Id,
                ServiceId = service.Id,
                ClientUserId = clientUserId,
                EmployeeId = null,
                Date = date,
                StartTime = start,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            db.WaitlistEntries.Add(Entry(WaitlistStatus.Waiting));
            await db.SaveChangesAsync();

            // A second Waiting entry for the same client+slot violates the filtered
            // unique index IX_WaitlistEntry_UniqueWaiting.
            db.WaitlistEntries.Add(Entry(WaitlistStatus.Waiting));
            await AssertUniqueViolationAsync(() => db.SaveChangesAsync());

            // The filter is WHERE Status = Waiting, so cancelling the first entry and
            // re-joining the same slot is allowed.
            db.ChangeTracker.Clear();
            var existing = await db.WaitlistEntries.FirstAsync(w => w.ClientUserId == clientUserId);
            existing.Status = WaitlistStatus.Cancelled;
            await db.SaveChangesAsync();

            db.WaitlistEntries.Add(Entry(WaitlistStatus.Waiting));
            await db.SaveChangesAsync(); // must NOT throw
        }

        // ----- helpers -----

        private static async Task AssertUniqueViolationAsync(Func<Task> act)
        {
            var ex = await Assert.ThrowsAsync<DbUpdateException>(act);
            // Assert it is specifically a unique-key violation (SQLSTATE 23505), not some
            // other DbUpdateException (e.g. a FK violation), so the test really exercises
            // the unique index and cannot false-pass.
            var pg = ex.InnerException as Npgsql.PostgresException;
            Assert.NotNull(pg);
            Assert.Equal(Npgsql.PostgresErrorCodes.UniqueViolation, pg!.SqlState);
        }

        private static ScheduleOverride NewOverride(int businessId, DateOnly date, string reason) => new()
        {
            BusinessId = businessId,
            Date = date,
            OverrideType = ScheduleOverrideType.Closed,
            Reason = reason,
            CreatedAt = DateTime.UtcNow
        };

        private static async Task<BusinessEntity> SeedBusinessAsync(AgendiaDbContext db)
        {
            var business = new BusinessEntity { IsActive = true, DefaultLanguage = "es" };
            db.Businesses.Add(business);
            await db.SaveChangesAsync();
            return business;
        }

        private static async Task<Service> SeedServiceAsync(AgendiaDbContext db, int businessId)
        {
            var service = new Service { BusinessId = businessId, DurationMinutes = 30 };
            db.Services.Add(service);
            await db.SaveChangesAsync();
            return service;
        }
    }
}
