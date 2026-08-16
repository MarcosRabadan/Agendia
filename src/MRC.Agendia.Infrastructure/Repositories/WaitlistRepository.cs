using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class WaitlistRepository : RepositoryBase<WaitlistEntry>, IWaitlistRepository
    {
        public WaitlistRepository(AgendiaDbContext context) : base(context)
        {
        }

        /// <inheritdoc />
        public Task<bool> ExistsWaitingAsync(
            string clientUserId, Guid businessId, Guid serviceId, DateOnly date, TimeOnly startTime, Guid? employeeId,
            CancellationToken cancellationToken = default)
            => Set.AnyAsync(w =>
                w.ClientUserId == clientUserId
                && w.BusinessId == businessId
                && w.ServiceId == serviceId
                && w.Date == date
                && w.StartTime == startTime
                && w.EmployeeId == employeeId
                && w.Status == WaitlistStatus.Waiting,
                cancellationToken);

        /// <inheritdoc />
        public Task<WaitlistEntry?> GetNextWaitingForSlotAsync(
            Guid businessId, Guid serviceId, DateOnly date, TimeOnly startTime, Guid employeeId,
            CancellationToken cancellationToken = default)
            // IgnoreQueryFilters + explicit liveness: never notify for a service that
            // was soft-deleted (BIZ-03). Tracked so the caller marks it Notified.
            => Set
                .IgnoreQueryFilters()
                .Where(w =>
                    w.Status == WaitlistStatus.Waiting
                    && w.BusinessId == businessId
                    && w.ServiceId == serviceId
                    && w.Date == date
                    && w.StartTime == startTime
                    && (w.EmployeeId == null || w.EmployeeId == employeeId)
                    && !w.Service.IsDeleted)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntry>> GetActiveHoldsAsync(Guid businessId,
                                                                            DateOnly date,
                                                                            DateTime nowUtc,
                                                                            CancellationToken cancellationToken = default)
            // Read-only: the availability calculation only subtracts these seats.
            => await Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(w => w.Status == WaitlistStatus.Notified
                    && w.BusinessId == businessId
                    && w.Date == date
                    && w.HoldUntil != null
                    && w.HoldUntil > nowUtc)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntry>> GetExpiredHoldsAsync(DateTime nowUtc,
                                                                             int batchSize,
                                                                             CancellationToken cancellationToken = default)
            // Tracked: the expiry job marks these Expired before moving the queue on.
            => await Set
                .IgnoreQueryFilters()
                .Where(w => w.Status == WaitlistStatus.Notified
                    && w.HoldUntil != null
                    && w.HoldUntil <= nowUtc)
                .OrderBy(w => w.HoldUntil)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public Task<WaitlistEntry?> GetActiveHoldForClientAsync(string clientUserId,
                                                                DateOnly date,
                                                                TimeOnly startTime,
                                                                Guid employeeId,
                                                                DateTime nowUtc,
                                                                CancellationToken cancellationToken = default)
            // An "any employee" hold (EmployeeId null) is consumed by booking that slot
            // with whichever employee, so it matches too.
            => Set
                .IgnoreQueryFilters()
                .Where(w => w.Status == WaitlistStatus.Notified
                    && w.ClientUserId == clientUserId
                    && w.Date == date
                    && w.StartTime == startTime
                    && (w.EmployeeId == null || w.EmployeeId == employeeId)
                    && w.HoldUntil != null
                    && w.HoldUntil > nowUtc)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntry>> GetActiveByClientUserIdAsync(string clientUserId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(w => w.ClientUserId == clientUserId && w.Status != WaitlistStatus.Cancelled)
                .OrderBy(w => w.Date)
                .ThenBy(w => w.StartTime)
                .ToListAsync(cancellationToken);
    }
}
