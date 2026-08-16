namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Shares one API host + Postgres container across the full-stack real-database
    /// test classes. Separate from <see cref="PostgresCollection"/> (the direct
    /// DbContext tests) so neither collection can truncate the other's data: xUnit
    /// serializes classes within a collection but runs collections in parallel.
    /// </summary>
    [CollectionDefinition(Name)]
    public class PostgresApiCollection : ICollectionFixture<PostgresWebApplicationFactory>
    {
        public const string Name = "postgres-api";
    }
}
