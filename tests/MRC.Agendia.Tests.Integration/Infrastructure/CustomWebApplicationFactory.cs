using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MRC.Agendia.Application.Common.Email;
using MRC.Agendia.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Test host for the API. Each fixture instance owns a unique in-memory
    /// database (the GUID is captured once in the constructor so every test in
    /// the same fixture sees the same data).
    ///
    /// Configuration is injected via environment variables so it is visible to
    /// <c>WebApplication.CreateBuilder(args)</c> in Program.cs BEFORE the
    /// startup pipeline reads <c>Jwt:Key</c>, the connection string, or the
    /// environment name. Overriding via <c>ConfigureAppConfiguration</c> would
    /// be applied too late: by then the host has already been built.
    ///
    /// Production code paths are honoured except for:
    ///   - Environment is "Testing" (PipelineExtensions skips HTTPS redirect
    ///     and rate limiter in that environment, see #47).
    ///   - DbContext is replaced with EF Core InMemory.
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"agendia-tests-{Guid.NewGuid()}";

        /// <summary>Captures emails so tests can read the reset/confirmation token.</summary>
        public FakeEmailSender EmailSender { get; } = new();

        public CustomWebApplicationFactory()
        {
            // 64-char deterministic key (good enough for HS256 in tests).
            Environment.SetEnvironmentVariable("Jwt__Key",
                "test-key-for-integration-tests-do-not-use-in-production-1234567890");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "MRC.Agendia.Tests");
            Environment.SetEnvironmentVariable("Jwt__Audience", "MRC.Agendia.Tests.Clients");
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("Jwt__RefreshTokenDays", "7");

            // Required by AddInfrastructure (registers SQL Server) even though
            // the DbContext is replaced below. SqlConnectionStringBuilder
            // refuses null/empty strings during DI graph construction.
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection",
                "Server=test-placeholder;Database=test;");

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Replace the SQL Server DbContext registered by AddInfrastructure
                // with an InMemory one shared across the whole fixture.
                services.RemoveAll<DbContextOptions<AgendiaDbContext>>();
                services.RemoveAll<AgendiaDbContext>();

                services.AddDbContext<AgendiaDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });

                // Capture outgoing emails instead of logging them, so tests can
                // read the reset/confirmation token out of the body.
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(EmailSender);
            });
        }
    }
}
