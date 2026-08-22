using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Availability;
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
        public async Task<IReadOnlyList<WaitlistEntry>> GetWaitingCandidatesForSlotAsync(
            Guid businessId,
            Guid serviceId,
            DateOnly date,
            TimeOnly? windowEnd,
            TimeOnly? earliestStart,
            Guid employeeId,
            int maxCandidates,
            CancellationToken cancellationToken = default)
            // IgnoreQueryFilters + explicit liveness: never notify for a service that
            // was soft-deleted (BIZ-03). Tracked so the caller marks one Notified.
            => await Set
                .IgnoreQueryFilters()
                .Where(w =>
                    w.Status == WaitlistStatus.Waiting
                    && w.BusinessId == businessId
                    && w.ServiceId == serviceId
                    && w.Date == date
                    // Overlap with the freed window, as two bounds on StartTime (#350).
                    && (windowEnd == null || w.StartTime < windowEnd)
                    && (earliestStart == null || w.StartTime > earliestStart)
                    && (w.EmployeeId == null || w.EmployeeId == employeeId)
                    && !w.Service.IsDeleted)
                .OrderBy(w => w.CreatedAt)
                .Take(maxCandidates)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<SlotHold>> GetActiveHoldsAsync(Guid businessId,
                                                                       DateOnly date,
                                                                       DateTime nowUtc,
                                                                       CancellationToken cancellationToken = default)
        {
            // Read-only: the callers only subtract these seats. IgnoreQueryFilters plus an
            // explicit liveness check, like GetWaitingCandidatesForSlotAsync: a hold on a
            // soft-deleted service holds nothing (BIZ-03). WaitlistEntry carries no soft
            // delete of its own, so there is nothing else to re-declare here.
            var rows = await Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(w => w.Status == WaitlistStatus.Notified
                    && w.BusinessId == businessId
                    && w.Date == date
                    && w.HoldUntil != null
                    && w.HoldUntil > nowUtc
                    && !w.Service.IsDeleted)
                .Select(w => new
                {
                    w.ClientUserId,
                    w.EmployeeId,
                    w.Date,
                    w.StartTime,
                    w.Service.DurationMinutes
                })
                .ToListAsync(cancellationToken);

            // The held window is the slot the client was offered, so it lasts as long as
            // THEIR service. Composed in memory to keep the projection translatable.
            return rows
                .Select(r =>
                {
                    var start = r.Date.ToDateTime(r.StartTime);
                    return new SlotHold(r.ClientUserId, r.EmployeeId, start, start.AddMinutes(r.DurationMinutes));
                })
                .ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntry>> GetExpiredHoldsAsync(DateTime nowUtc,
                                                                             int batchSize,
                                                                             CancellationToken cancellationToken = default)
            // Tracked: the expiry job marks these Expired before moving the queue on. Service
            // comes along because the job needs its duration to work out which other entries
            // overlap the slot it is freeing (#350).
            => await Set
                .IgnoreQueryFilters()
                .Include(w => w.Service)
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
