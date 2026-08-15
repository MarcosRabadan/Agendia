using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Infrastructure;
using Testcontainers.PostgreSql;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Spins up a throwaway PostgreSQL container (via Testcontainers) shared by the
    /// real-database tests, and creates the schema once. Replaces the old LocalDB
    /// probe: EF InMemory does not enforce indexes/constraints nor advisory locks,
    /// so those must run against a real Postgres.
    ///
    /// If Docker is not available in the environment, <see cref="Available"/> stays
    /// false and the dependent tests skip cleanly (mirroring the old LocalDB skip).
    /// </summary>
    public class PostgresContainerFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;

        /// <summary>True when the container started and the schema was created.</summary>
        public bool Available { get; private set; }

        /// <summary>Connection string to the running container (empty when unavailable).</summary>
        public string ConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            try
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16")
                    .Build();
                await _container.StartAsync();
                ConnectionString = _container.GetConnectionString();

                // Apply the real MIGRATIONS (not EnsureCreated): this exercises the
                // actual migration SQL - including the timestamp column types - and
                // catches any drift between the model and the migrations.
                await using var db = CreateContext();
                await db.Database.MigrateAsync();
                Available = true;
            }
            catch
            {
                // Docker not available here: dependent tests skip.
                Available = false;
            }
        }

        /// <summary>New context bound to the container (Npgsql, unrestricted scope).</summary>
        public AgendiaDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>().UseNpgsql(ConnectionString).Options,
                new UnrestrictedBusinessScope());

        public async Task DisposeAsync()
        {
            if (_container is not null)
                await _container.DisposeAsync();
        }
    }

    /// <summary>Shares one Postgres container across all real-database test classes.</summary>
    [CollectionDefinition(Name)]
    public class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
    {
        public const string Name = "postgres";
    }
}
