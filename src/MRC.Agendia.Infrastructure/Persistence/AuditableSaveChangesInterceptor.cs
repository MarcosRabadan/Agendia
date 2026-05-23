using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Common;

namespace MRC.Agendia.Infrastructure.Persistence
{
    /// <summary>
    /// Fills audit fields (<see cref="IAuditable"/>) and turns physical deletes
    /// of <see cref="ISoftDelete"/> entities into soft deletes, transparently for
    /// every call to <c>SaveChanges</c>.
    /// </summary>
    public class AuditableSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserContext _currentUser;

        public AuditableSaveChangesInterceptor(ICurrentUserContext currentUser)
        {
            _currentUser = currentUser;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyChanges(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyChanges(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyChanges(DbContext? context)
        {
            if (context is null) return;

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is ISoftDelete softDelete && entry.State == EntityState.Deleted)
                {
                    // Never physically remove: flip the flag and treat it as an update.
                    entry.State = EntityState.Modified;
                    softDelete.IsDeleted = true;
                    softDelete.DeletedAt = now;
                }

                if (entry.Entity is IAuditable auditable)
                {
                    ApplyAuditFields(entry, auditable, now, userId);
                }
            }
        }

        private static void ApplyAuditFields(
            EntityEntry entry, IAuditable auditable, DateTime now, string? userId)
        {
            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userId;
            }
        }
    }
}
