using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Persistence;
using MRC.Agendia.Tests.Unit.TestDoubles;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Persistence
{
    public class AuditableSaveChangesInterceptorTests
    {
        private static AgendiaDbContext NewContext(ICurrentUserContext user, string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .AddInterceptors(new AuditableSaveChangesInterceptor(user))
                .Options, new UnrestrictedBusinessScope());

        [Fact]
        public async Task Insert_RellenaCreatedAtYCreatedBy()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";
            using var ctx = NewContext(new FakeCurrentUserContext { UserId = "creator" }, dbName);

            var service = new Service { DurationMinutes = 30 };
            ctx.Services.Add(service);
            await ctx.SaveChangesAsync();

            Assert.NotEqual(default, service.CreatedAt);
            Assert.Equal("creator", service.CreatedBy);
            Assert.Null(service.UpdatedAt);
            Assert.Null(service.UpdatedBy);
        }

        [Fact]
        public async Task Update_RellenaUpdatedAtYUpdatedBy_SinTocarCreated()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "creator" }, dbName))
            {
                ctx.Services.Add(new Service { DurationMinutes = 30 });
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "editor" }, dbName))
            {
                var service = await ctx.Services.SingleAsync();
                service.DurationMinutes = 45;
                await ctx.SaveChangesAsync();

                Assert.Equal("creator", service.CreatedBy);
                Assert.Equal("editor", service.UpdatedBy);
                Assert.NotNull(service.UpdatedAt);
            }
        }

        [Fact]
        public async Task Delete_ConvierteEnSoftDelete_OcultoPorQueryFilter()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                ctx.Services.Add(new Service { DurationMinutes = 30 });
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                var service = await ctx.Services.SingleAsync();
                ctx.Services.Remove(service);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                // The global query filter hides soft-deleted rows.
                Assert.False(await ctx.Services.AnyAsync());

                // The row is still physically present, just flagged.
                var deleted = await ctx.Services.IgnoreQueryFilters().SingleAsync();
                Assert.True(deleted.IsDeleted);
                Assert.NotNull(deleted.DeletedAt);
            }
        }
    }
}
