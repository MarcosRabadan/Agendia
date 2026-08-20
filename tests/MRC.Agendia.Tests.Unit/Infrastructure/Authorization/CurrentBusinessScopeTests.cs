using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Authorization;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Authorization
{
    /// <summary>
    /// The multi-tenant scope behind the global business filter (#58), and its asynchronous
    /// pre-resolution (#313).
    ///
    /// <para>Two things matter here. First, WHO is restricted is decided by role, never by
    /// whether the lookup found rows. Second, the eager path the middleware calls and the lazy
    /// fallback must agree exactly: they are two entry points into one decision, and the whole
    /// point of the change is that a request takes the eager one - a disagreement would mean
    /// the same caller sees different data depending on the route in.</para>
    /// </summary>
    public class CurrentBusinessScopeTests
    {
        private const string Owner = "harmony-owner";

        private static AgendiaDbContext NewDb()
            => new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase($"scope-{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options, new UnrestrictedBusinessScope());

        private static ICurrentUserContext User(bool authenticated, string? userId, params string[] roles)
        {
            var user = Substitute.For<ICurrentUserContext>();
            user.IsAuthenticated.Returns(authenticated);
            user.UserId.Returns(userId);
            user.IsInRole(Arg.Any<string>()).Returns(call => roles.Contains(call.Arg<string>()));
            return user;
        }

        private static IServiceScopeFactory ScopeFactoryOver(AgendiaDbContext db)
        {
            var provider = Substitute.For<IServiceProvider>();
            provider.GetService(typeof(AgendiaDbContext)).Returns(db);

            var scope = Substitute.For<IServiceScope>();
            scope.ServiceProvider.Returns(provider);

            var factory = Substitute.For<IServiceScopeFactory>();
            factory.CreateScope().Returns(scope);
            return factory;
        }

        /// <summary>A business owned by <see cref="Owner"/>, plus one owned by somebody else.</summary>
        private static async Task<(AgendiaDbContext Db, Guid Mine, Guid Theirs)> SeededAsync()
        {
            var db = NewDb();

            var mine = new BusinessEntity { IsActive = true, DefaultLanguage = "es", OwnerUserId = Owner };
            var theirs = new BusinessEntity { IsActive = true, DefaultLanguage = "es", OwnerUserId = "harmony-someone-else" };
            db.AddRange(mine, theirs);
            await db.SaveChangesAsync();

            return (db, mine.Id, theirs.Id);
        }

        private static CurrentBusinessScope Sut(ICurrentUserContext user, AgendiaDbContext db)
            => new(user, ScopeFactoryOver(db));

        [Theory]
        [InlineData(false, null)]                 // anonymous
        [InlineData(true, Roles.Admin)]           // admin / M2M: cross-tenant by design
        [InlineData(true, Roles.Client)]          // a client belongs to no business
        public async Task Callers_that_are_not_tenant_bound_are_never_restricted(bool authenticated, string? role)
        {
            var (db, _, _) = await SeededAsync();
            var roles = role is null ? Array.Empty<string>() : new[] { role };
            var sut = Sut(User(authenticated, Owner, roles), db);

            await sut.EnsureResolvedAsync();

            Assert.False(sut.IsRestricted);
            Assert.Empty(sut.BusinessIds);
        }

        [Fact]
        public async Task An_owner_is_restricted_to_their_own_business()
        {
            var (db, mine, theirs) = await SeededAsync();
            var sut = Sut(User(true, Owner, Roles.BusinessOwner), db);

            await sut.EnsureResolvedAsync();

            Assert.True(sut.IsRestricted);
            Assert.Equal(new[] { mine }, sut.BusinessIds);
            Assert.DoesNotContain(theirs, sut.BusinessIds);
        }

        [Fact]
        public async Task An_employee_is_restricted_to_the_business_they_work_at()
        {
            var (db, mine, _) = await SeededAsync();
            db.Employees.Add(new Employee { BusinessId = mine, UserId = "harmony-teacher", IsActive = true });
            await db.SaveChangesAsync();

            var sut = Sut(User(true, "harmony-teacher", Roles.Employee), db);

            await sut.EnsureResolvedAsync();

            Assert.True(sut.IsRestricted);
            Assert.Equal(new[] { mine }, sut.BusinessIds);
        }

        [Fact]
        public async Task A_tenant_bound_caller_with_no_rows_is_restricted_to_nothing_not_to_everything()
        {
            // Harmony issues roles independently, so a token can carry BusinessOwner before the
            // matching row is provisioned here. That must not read as "unrestricted".
            var (db, _, _) = await SeededAsync();
            var sut = Sut(User(true, "harmony-nobody", Roles.BusinessOwner), db);

            await sut.EnsureResolvedAsync();

            Assert.True(sut.IsRestricted);
            Assert.Empty(sut.BusinessIds);
        }

        [Fact]
        public async Task An_authenticated_token_without_a_subject_gets_the_empty_scope()
        {
            var (db, _, _) = await SeededAsync();
            var sut = Sut(User(true, null, Roles.BusinessOwner), db);

            await sut.EnsureResolvedAsync();

            Assert.True(sut.IsRestricted);
            Assert.Empty(sut.BusinessIds);
        }

        [Fact]
        public async Task The_eager_path_and_the_lazy_fallback_agree()
        {
            // The middleware takes the eager path and everything else falls back to the lazy
            // one. If they ever disagreed, the same caller would see different data depending
            // on how the scope happened to be resolved first.
            var (eagerDb, mine, _) = await SeededAsync();
            var eager = Sut(User(true, Owner, Roles.BusinessOwner), eagerDb);
            await eager.EnsureResolvedAsync();

            var (lazyDb, lazyMine, _) = await SeededAsync();
            var lazy = Sut(User(true, Owner, Roles.BusinessOwner), lazyDb);
            // No EnsureResolvedAsync: reading the property is what resolves it.
            var lazyIds = lazy.BusinessIds;

            Assert.Equal(eager.IsRestricted, lazy.IsRestricted);
            Assert.Equal(new[] { mine }, eager.BusinessIds);
            Assert.Equal(new[] { lazyMine }, lazyIds);
        }

        [Fact]
        public async Task Resolving_twice_only_queries_once()
        {
            var (db, mine, _) = await SeededAsync();
            var factory = ScopeFactoryOver(db);
            var sut = new CurrentBusinessScope(User(true, Owner, Roles.BusinessOwner), factory);

            await sut.EnsureResolvedAsync();
            await sut.EnsureResolvedAsync();
            _ = sut.BusinessIds;

            // One scope created means one lookup: the middleware calling this on every request
            // must not turn into repeated round-trips.
            factory.Received(1).CreateScope();
            Assert.Equal(new[] { mine }, sut.BusinessIds);
        }
    }
}
