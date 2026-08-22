using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Repositories;
using MRC.Agendia.Tests.Unit.TestDoubles;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Repositories
{
    /// <summary>
    /// The candidate query behind the waitlist notification (#350), condition by condition.
    ///
    /// <para>It carries eight of them - status, business, service, date, the two overlap bounds,
    /// the employee match and the service liveness - plus an order and a cap, and until now only
    /// the two that the notification flow happens to exercise were covered. Everything else could
    /// be dropped in an edit without a single test going red, on the query that decides whether a
    /// student ever hears from the queue they joined.</para>
    /// </summary>
    public class WaitlistRepositoryTests
    {
        private static readonly DateOnly Day = new(2035, 6, 4);
        private static readonly Guid BusinessId = TestIds.Of(1);
        private static readonly Guid ServiceId = TestIds.Of(2);
        private static readonly Guid EmployeeId = TestIds.Of(3);

        // A freed 10:00-11:00 class of a 60 minute service: anybody starting after 09:00 and
        // before 11:00 is still running while it is free.
        private static readonly TimeOnly WindowEnd = new(11, 0);
        private static readonly TimeOnly EarliestStart = new(9, 0);

        private static AgendiaDbContext NewContext(string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options, new UnrestrictedBusinessScope());

        /// <summary>
        /// The bounds are EXCLUSIVE on both sides, which is the whole point: a slot that ends
        /// exactly when the freed one starts, or starts exactly when it ends, only touches it.
        /// </summary>
        [Fact]
        public async Task Candidatos_DevuelveLosQueSolapanYNoLosQueSoloRozan()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "ends-when-it-starts", new TimeOnly(9, 0));
            Add(ctx, "same-hour", new TimeOnly(10, 0));
            Add(ctx, "overlaps-halfway", new TimeOnly(10, 30));
            Add(ctx, "starts-when-it-ends", new TimeOnly(11, 0));
            await ctx.SaveChangesAsync();

            var candidates = await QueryAsync(ctx);

            Assert.Equal(
                new[] { "overlaps-halfway", "same-hour" },
                candidates.Select(c => c.ClientUserId).OrderBy(c => c).ToArray());
        }

        /// <summary>
        /// A null bound means "unbounded on that side", not "match nothing": it is what a window
        /// running past midnight, or opening less than one duration after it, really means.
        /// </summary>
        [Fact]
        public async Task Candidatos_SinCotas_NoFiltraPorHora()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "early", new TimeOnly(0, 15));
            Add(ctx, "late", new TimeOnly(23, 45));
            await ctx.SaveChangesAsync();

            var candidates = await QueryAsync(ctx, unbounded: true);

            Assert.Equal(2, candidates.Count);
        }

        /// <summary>
        /// An "any employee" entry wants the slot from whoever; an entry naming a different
        /// teacher is not served by this one's cancellation.
        /// </summary>
        [Fact]
        public async Task Candidatos_IncluyeCualquierEmpleadoYExcluyeOtroEmpleado()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "any-employee", new TimeOnly(10, 30), anyEmployee: true);
            Add(ctx, "this-employee", new TimeOnly(10, 30));
            Add(ctx, "another-employee", new TimeOnly(10, 30), employeeId: TestIds.Of(99));
            await ctx.SaveChangesAsync();

            var candidates = await QueryAsync(ctx);

            Assert.Equal(2, candidates.Count);
            Assert.DoesNotContain(candidates, c => c.ClientUserId == "another-employee");
        }

        [Theory]
        [InlineData(WaitlistStatus.Notified)]
        [InlineData(WaitlistStatus.Cancelled)]
        [InlineData(WaitlistStatus.Expired)]
        [InlineData(WaitlistStatus.Booked)]
        public async Task Candidatos_SoloDevuelveLasQueSiguenWaiting(WaitlistStatus status)
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "not-waiting", new TimeOnly(10, 30), status: status);
            await ctx.SaveChangesAsync();

            Assert.Empty(await QueryAsync(ctx));
        }

        [Fact]
        public async Task Candidatos_ExcluyeOtroNegocioOtroServicioYOtroDia()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "other-business", new TimeOnly(10, 30), businessId: TestIds.Of(77));
            Add(ctx, "other-service", new TimeOnly(10, 30), serviceId: TestIds.Of(88));
            Add(ctx, "other-day", new TimeOnly(10, 30), date: Day.AddDays(1));
            Add(ctx, "the-one", new TimeOnly(10, 30));
            await ctx.SaveChangesAsync();

            var candidate = Assert.Single(await QueryAsync(ctx));
            Assert.Equal("the-one", candidate.ClientUserId);
        }

        /// <summary>
        /// BIZ-03: the filters come off so a soft-deleted parent cannot drop the row, so the
        /// liveness of the service has to be re-stated by hand - never call somebody for a class
        /// the academy no longer offers.
        /// </summary>
        [Fact]
        public async Task Candidatos_ExcluyeLasDeUnServicioBorrado()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "on-a-dead-service", new TimeOnly(10, 30));
            await ctx.SaveChangesAsync();

            var service = await ctx.Services.FirstAsync(s => s.Id == ServiceId);
            service.IsDeleted = true;
            await ctx.SaveChangesAsync();

            Assert.Empty(await QueryAsync(ctx));
        }

        /// <summary>
        /// FIFO by CreatedAt, and no more than asked: the walk that follows costs a capacity read
        /// per candidate inside the booking lock, so the cap is what bounds that section.
        /// </summary>
        [Fact]
        public async Task Candidatos_OrdenaPorAntiguedadYRespetaElTope()
        {
            using var ctx = NewContext($"wl-repo-{Guid.NewGuid()}");
            await SeedAsync(ctx);
            Add(ctx, "third", new TimeOnly(10, 45), createdAt: new DateTime(2035, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            Add(ctx, "first", new TimeOnly(10, 15), createdAt: new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Add(ctx, "second", new TimeOnly(10, 30), createdAt: new DateTime(2035, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            await ctx.SaveChangesAsync();

            var all = await QueryAsync(ctx);
            Assert.Equal(new[] { "first", "second", "third" }, all.Select(c => c.ClientUserId).ToArray());

            var capped = await QueryAsync(ctx, maxCandidates: 2);
            Assert.Equal(new[] { "first", "second" }, capped.Select(c => c.ClientUserId).ToArray());
        }

        // ----- Helpers -----

        /// <param name="unbounded">
        /// True to pass no bounds at all, the day-edge case. A flag and not a nullable argument
        /// on purpose: with <c>windowEnd ?? WindowEnd</c> the "no bound" case would silently
        /// query WITH the default bounds, and the test would pass by testing nothing.
        /// </param>
        private static Task<IReadOnlyList<WaitlistEntry>> QueryAsync(AgendiaDbContext ctx,
                                                                     bool unbounded = false,
                                                                     int maxCandidates = 10)
            => new WaitlistRepository(ctx).GetWaitingCandidatesForSlotAsync(
                BusinessId, ServiceId, Day,
                unbounded ? null : WindowEnd,
                unbounded ? null : EarliestStart,
                EmployeeId, maxCandidates);

        private static async Task SeedAsync(AgendiaDbContext ctx)
        {
            ctx.Businesses.Add(new Business { Id = BusinessId, IsActive = true });
            ctx.Services.Add(new Service { Id = ServiceId, BusinessId = BusinessId, DurationMinutes = 60 });
            ctx.Services.Add(new Service { Id = TestIds.Of(88), BusinessId = BusinessId, DurationMinutes = 60 });
            ctx.Employees.Add(new Employee { Id = EmployeeId, BusinessId = BusinessId, IsActive = true, MaxConcurrentAppointments = 1 });
            await ctx.SaveChangesAsync();
        }

        /// <param name="anyEmployee">
        /// True for an entry that takes the slot from whoever (EmployeeId null). A separate flag
        /// rather than a null employeeId, so "not specified" and "explicitly nobody" stay apart.
        /// </param>
        private static void Add(AgendiaDbContext ctx,
                                string clientUserId,
                                TimeOnly startTime,
                                Guid? employeeId = null,
                                bool anyEmployee = false,
                                Guid? businessId = null,
                                Guid? serviceId = null,
                                DateOnly? date = null,
                                WaitlistStatus status = WaitlistStatus.Waiting,
                                DateTime? createdAt = null)
            => ctx.WaitlistEntries.Add(new WaitlistEntry
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId ?? BusinessId,
                ServiceId = serviceId ?? ServiceId,
                EmployeeId = anyEmployee ? null : employeeId ?? EmployeeId,
                ClientUserId = clientUserId,
                Date = date ?? Day,
                StartTime = startTime,
                Status = status,
                CreatedAt = createdAt ?? new DateTime(2035, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
    }
}
