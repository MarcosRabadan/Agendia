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
                .Options);

        [Fact]
        public async Task Insert_RellenaCreatedAtYCreatedBy()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";
            using var ctx = NewContext(new FakeCurrentUserContext { UserId = "creator" }, dbName);

            var client = new Client { Name = "Ana", Phone = "600" };
            ctx.Clients.Add(client);
            await ctx.SaveChangesAsync();

            Assert.NotEqual(default, client.CreatedAt);
            Assert.Equal("creator", client.CreatedBy);
            Assert.Null(client.UpdatedAt);
            Assert.Null(client.UpdatedBy);
        }

        [Fact]
        public async Task Update_RellenaUpdatedAtYUpdatedBy_SinTocarCreated()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "creator" }, dbName))
            {
                ctx.Clients.Add(new Client { Name = "Ana", Phone = "600" });
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "editor" }, dbName))
            {
                var client = await ctx.Clients.SingleAsync();
                client.Name = "Ana B";
                await ctx.SaveChangesAsync();

                Assert.Equal("creator", client.CreatedBy);
                Assert.Equal("editor", client.UpdatedBy);
                Assert.NotNull(client.UpdatedAt);
            }
        }

        [Fact]
        public async Task Delete_ConvierteEnSoftDelete_OcultoPorQueryFilter()
        {
            var dbName = $"audit-int-{Guid.NewGuid()}";

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                ctx.Clients.Add(new Client { Name = "Ana", Phone = "600" });
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                var client = await ctx.Clients.SingleAsync();
                ctx.Clients.Remove(client);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(new FakeCurrentUserContext { UserId = "u" }, dbName))
            {
                // The global query filter hides soft-deleted rows.
                Assert.False(await ctx.Clients.AnyAsync());

                // The row is still physically present, just flagged.
                var deleted = await ctx.Clients.IgnoreQueryFilters().SingleAsync();
                Assert.True(deleted.IsDeleted);
                Assert.NotNull(deleted.DeletedAt);
            }
        }
    }
}
