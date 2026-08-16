using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Empties the database between tests that share a Postgres container.
    ///
    /// The table list is derived from the EF model instead of being hard-coded, so a
    /// new entity is covered automatically and a renamed one cannot leave stale rows
    /// behind. <c>__EFMigrationsHistory</c> is not part of the model, so it survives
    /// and the schema is migrated only once per container.
    ///
    /// A single TRUNCATE of all tables at once satisfies the foreign keys without any
    /// ordering work; RESTART IDENTITY resets the only sequence-backed key
    /// (AuditLog.Id - every other key is a client-generated UUIDv7).
    /// </summary>
    public static class PostgresDatabaseReset
    {
        public static async Task TruncateAllAsync(AgendiaDbContext context,
                                                  CancellationToken cancellationToken = default)
        {
            var tables = context.Model.GetEntityTypes()
                .Select(entityType => entityType.GetTableName())
                .Where(table => !string.IsNullOrEmpty(table))
                .Distinct()
                .Select(table => $"\"{table}\"")
                .ToList();

            if (tables.Count == 0)
                return;

            // Table names come from the EF model (compile-time metadata, never user
            // input), so composing them into the statement is safe here. Built as a
            // plain string rather than an interpolated one on purpose: the EF1002
            // analyzer flags interpolation at the call site, and identifiers cannot
            // be passed as parameters anyway.
            var sql = "TRUNCATE TABLE " + string.Join(", ", tables) + " RESTART IDENTITY CASCADE;";

            await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        }
    }
}
