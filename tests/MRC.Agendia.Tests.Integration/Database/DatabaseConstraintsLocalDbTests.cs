using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Identity;
using MRC.Agendia.Tests.Integration.Infrastructure;
// The sibling "...Integration.Business" namespace (a test folder) shadows the entity.
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Integration.Database
{
    /// <summary>
    /// Verifies the unique/filtered indexes against a REAL SQL Server (LocalDB).
    /// EF InMemory does not enforce indexes/constraints, so a SQL-only bug (e.g. the
    /// schedule-override unique index that produced a 500 in #188) passes unnoticed
    /// under the green InMemory suite. EnsureCreatedAsync builds the full model schema
    /// here - indexes included - so these duplicate inserts must be rejected.
    ///
    /// Skipped automatically when LocalDB is unreachable (CI without SQL Server),
    /// mirroring <c>BookingConcurrencyGuardLocalDbTests</c>.
    /// </summary>
    public class DatabaseConstraintsLocalDbTests
    {
        private const string LocalDbBase =
            @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;MultipleActiveResultSets=true;Connect Timeout=5";

        [SkippableFact]
        public async Task ScheduleOverride_DuplicateBusinessDate_IsRejected()
        {
            await WithLocalDbAsync(async db =>
            {
                var business = await SeedBusinessAsync(db);
                var date = new DateOnly(2026, 12, 25);

                db.ScheduleOverrides.Add(NewOverride(business.Id, date, "Navidad"));
                await db.SaveChangesAsync();

                // IX_ScheduleOverride_BusinessId_Date is unique: a second override for the
                // same (business, date) violates it. This is the #188 scenario that the
                // InMemory suite could not catch.
                db.ScheduleOverrides.Add(NewOverride(business.Id, date, "Duplicado"));
                await AssertUniqueViolationAsync(() => db.SaveChangesAsync());
            });
        }

        [SkippableFact]
        public async Task WaitlistEntry_DuplicateActiveSlot_IsRejected_ButReJoinAfterCancelIsAllowed()
        {
            await WithLocalDbAsync(async db =>
            {
                var business = await SeedBusinessAsync(db);
                var service = await SeedServiceAsync(db, business.Id);
                var client = await SeedClientAsync(db);
                var date = new DateOnly(2026, 6, 7);
                var start = new TimeOnly(16, 0);

                WaitlistEntry Entry(WaitlistStatus status) => new()
                {
                    BusinessId = business.Id,
                    ServiceId = service.Id,
                    ClientId = client.Id,
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
                var existing = await db.WaitlistEntries.FirstAsync(w => w.ClientId == client.Id);
                existing.Status = WaitlistStatus.Cancelled;
                await db.SaveChangesAsync();

                db.WaitlistEntries.Add(Entry(WaitlistStatus.Waiting));
                await db.SaveChangesAsync(); // must NOT throw
            });
        }

        [SkippableFact]
        public async Task RefreshToken_DuplicateTokenValue_IsRejected()
        {
            await WithLocalDbAsync(async db =>
            {
                var user = await SeedUserAsync(db);
                const string token = "duplicate-token-value";

                db.RefreshTokens.Add(new RefreshToken { Token = token, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(7) });
                await db.SaveChangesAsync();

                // IX_RefreshToken_Token is unique: the duplicate value is rejected.
                db.RefreshTokens.Add(new RefreshToken { Token = token, UserId = user.Id, ExpiresAt = DateTime.UtcNow.AddDays(7) });
                await AssertUniqueViolationAsync(() => db.SaveChangesAsync());
            });
        }

        // ----- helpers -----

        private static async Task AssertUniqueViolationAsync(Func<Task> act)
        {
            var ex = await Assert.ThrowsAsync<DbUpdateException>(act);
            // Assert it is specifically a unique-key violation (2601 index / 2627
            // constraint), not some other DbUpdateException (e.g. a FK violation), so
            // the test really exercises the unique index and cannot false-pass.
            var sql = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
            Assert.NotNull(sql);
            Assert.Contains(sql!.Number, new[] { 2601, 2627 });
        }

        private async Task WithLocalDbAsync(Func<AgendiaDbContext, Task> body)
        {
            var connectionString = $"{LocalDbBase};Database=agendia-constraints-{Guid.NewGuid():N}";

            AgendiaDbContext NewContext() =>
                new(new DbContextOptionsBuilder<AgendiaDbContext>().UseSqlServer(connectionString).Options,
                    new UnrestrictedBusinessScope());

            var available = false;
            try
            {
                await using var probe = NewContext();
                await probe.Database.EnsureCreatedAsync();
                available = true;
            }
            catch
            {
                // LocalDB not installed / not reachable here.
            }

            Skip.IfNot(available, "LocalDB no disponible; test de constraints omitido.");

            try
            {
                await using var db = NewContext();
                await body(db);
            }
            finally
            {
                try
                {
                    await using var teardown = NewContext();
                    await teardown.Database.EnsureDeletedAsync();
                }
                catch
                {
                    // Best-effort cleanup of the throwaway test database.
                }
            }
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
            var business = new BusinessEntity { Name = "B", Address = "A", Phone = "P", Email = "b@test", IsActive = true, DefaultLanguage = "es" };
            db.Businesses.Add(business);
            await db.SaveChangesAsync();
            return business;
        }

        private static async Task<Service> SeedServiceAsync(AgendiaDbContext db, int businessId)
        {
            var service = new Service { BusinessId = businessId, Name = "S", DurationMinutes = 30, Price = 10m };
            db.Services.Add(service);
            await db.SaveChangesAsync();
            return service;
        }

        private static async Task<Client> SeedClientAsync(AgendiaDbContext db)
        {
            var client = new Client { Name = "C", Phone = "P", Email = "c@test" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
            return client;
        }

        private static async Task<ApplicationUser> SeedUserAsync(AgendiaDbContext db)
        {
            var user = new ApplicationUser { UserName = $"u-{Guid.NewGuid():N}", Email = "u@test", FullName = "U", IsActive = true };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }
    }
}
