using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Infrastructure.Notifications;
using Testcontainers.PostgreSql;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Test host for the API running against a REAL PostgreSQL (Testcontainers), the
    /// bridge between the two worlds the suite used to have: full-stack HTTP tests on
    /// EF InMemory (no column types, constraints, indexes, DateTime.Kind, transactions
    /// or advisory locks) and real-Postgres tests that never went through the API.
    ///
    /// <para><b>When to use it.</b> Only for tests that need database fidelity: the real
    /// booking guard (pg_advisory_xact_lock inside a transaction), wall-clock timestamp
    /// columns, unique indexes/constraints, the outbox write. Everything else stays on
    /// <see cref="CustomWebApplicationFactory"/>, which is much faster and remains the
    /// default path for the bulk of the suite.</para>
    ///
    /// <para><b>How the connection string gets in.</b> Unlike the InMemory factory (which
    /// forces its settings through environment variables), the container's connection
    /// string is injected as a configuration source: <c>AddInfrastructure</c> reads it
    /// inside the <c>AddDbContext</c> callback, which runs lazily when the first scope
    /// resolves the context, i.e. long after the host was built. An environment variable
    /// would also be shared process-wide with <see cref="CustomWebApplicationFactory"/>
    /// (whose constructor writes a placeholder), and collections run in parallel.</para>
    ///
    /// <para><b>Isolation.</b> One container per collection, shared by every class in it
    /// (xUnit serializes classes inside a collection). Call
    /// <see cref="ResetDatabaseAsync"/> at the start of each test to truncate every mapped
    /// table (see <see cref="PostgresDatabaseReset"/>); the schema and the migration
    /// history survive, so migrations are applied only once.</para>
    ///
    /// <para>Production wiring is honoured except for: the "Testing" environment (skips
    /// HTTPS redirect), ephemeral DataProtection keys (portable to a locked-down CI), and
    /// the two background loops, which are removed so their polling neither races the
    /// truncation nor consumes the outbox rows a test is about to assert on.</para>
    ///
    /// Skips cleanly when Docker is unavailable: <see cref="Available"/> stays false.
    /// </summary>
    public class PostgresWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        private string _connectionString = string.Empty;

        /// <summary>True when the container started and the migrations were applied.</summary>
        public bool Available { get; private set; }

        public PostgresWebApplicationFactory()
        {
            // Same signing material TestTokenFactory forges tokens with: it stands in
            // for the secret shared with the Harmony identity service. These must be
            // environment variables because Program.cs reads them while the host is
            // being built (see CustomWebApplicationFactory), and both factories set
            // the very same values, so sharing the process environment is harmless.
            Environment.SetEnvironmentVariable("Jwt__Key", TestTokenFactory.Key);
            Environment.SetEnvironmentVariable("Jwt__Issuer", TestTokenFactory.Issuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", TestTokenFactory.Audience);

            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        }

        public async Task InitializeAsync()
        {
            try
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16")
                    .Build();
                await _container.StartAsync();
                _connectionString = _container.GetConnectionString();
            }
            catch
            {
                // Docker not available here: dependent tests skip. Only the container
                // start is swallowed - a failure past this point (a broken migration,
                // a host that will not build) must fail loudly instead of quietly
                // skipping the tests that would have caught it.
                Available = false;
                return;
            }

            // Touching Services builds the host (now that the connection string is
            // known). Apply the real MIGRATIONS, not EnsureCreated: that is what
            // exercises the migration SQL - column types included - and what
            // production runs.
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            await db.Database.MigrateAsync();

            Available = true;
        }

        /// <summary>
        /// Empties every mapped table so each test starts from a known state. Cheap
        /// enough to run per test (a single TRUNCATE), and it leaves the schema and
        /// __EFMigrationsHistory untouched.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            await PostgresDatabaseReset.TruncateAllAsync(db);
        }

        /// <summary>New context bound to the container, for arranging or asserting
        /// straight against the database (unrestricted scope, no query filters by tenant).</summary>
        public AgendiaDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>().UseNpgsql(_connectionString).Options,
                new UnrestrictedBusinessScope());

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
            });

            builder.ConfigureServices(services =>
            {
                // Ephemeral DataProtection keys: never touch the on-disk key ring,
                // so the suite is portable to a locked-down CI.
                services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();

                // Background loops off: against a real database the outbox dispatcher
                // would deliver (and stamp as processed) the very rows a test asserts
                // on, and both loops would keep querying tables while another test
                // truncates them. Their logic has its own dedicated Postgres tests.
                RemoveHostedService<OutboxDispatcherService>(services);
                RemoveHostedService<AppointmentReminderService>(services);
            });
        }

        private static void RemoveHostedService<THostedService>(IServiceCollection services)
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                            && d.ImplementationType == typeof(THostedService))
                .ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);
        }

        // Explicit implementation: the base class already exposes an IAsyncDisposable
        // DisposeAsync (different return type), so both cannot share a signature.
        async Task IAsyncLifetime.DisposeAsync()
        {
            // Tear the host down first: it still holds pooled connections to the
            // container we are about to kill.
            await base.DisposeAsync();

            if (_container is not null)
                await _container.DisposeAsync();
        }
    }
}
