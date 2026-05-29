using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Auditing;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Auditing
{
    public class AuditLoggerTests
    {
        private static AgendiaDbContext NewContext() =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase($"audit-{Guid.NewGuid()}")
                .Options, new UnrestrictedBusinessScope());

        [Fact]
        public async Task LogAsync_PersisteEntrada_ConUsuarioIpYDetails()
        {
            using var ctx = NewContext();
            var currentUser = new FakeCurrentUserContext { UserId = "u1", IpAddress = "1.2.3.4" };
            var sut = new AuditLogger(ctx, currentUser, Substitute.For<ILogger<AuditLogger>>());

            await sut.LogAsync("LOGIN_SUCCESS", "User", "u1", new { email = "a@b.com" });

            var entry = await ctx.AuditLogs.SingleAsync();
            Assert.Equal("LOGIN_SUCCESS", entry.Action);
            Assert.Equal("u1", entry.UserId);
            Assert.Equal("1.2.3.4", entry.IpAddress);
            Assert.Equal("User", entry.EntityType);
            Assert.Contains("a@b.com", entry.Details);
        }

        [Fact]
        public async Task LogAsync_EsBestEffort_NoLanzaSiFalla()
        {
            // A disposed context makes the write fail; LogAsync must swallow it.
            var ctx = NewContext();
            await ctx.DisposeAsync();
            var sut = new AuditLogger(ctx, new FakeCurrentUserContext(), Substitute.For<ILogger<AuditLogger>>());

            await sut.LogAsync("ANYTHING");
        }
    }
}
