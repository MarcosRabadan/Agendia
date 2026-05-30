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
            int clientId, int businessId, int serviceId, DateOnly date, TimeOnly startTime, int? employeeId,
            CancellationToken cancellationToken = default)
            => Set.AnyAsync(w =>
                w.ClientId == clientId
                && w.BusinessId == businessId
                && w.ServiceId == serviceId
                && w.Date == date
                && w.StartTime == startTime
                && w.EmployeeId == employeeId
                && w.Status == WaitlistStatus.Waiting,
                cancellationToken);

        /// <inheritdoc />
        public Task<WaitlistEntry?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            // Required Client/Service navigations keep their soft-delete filters, so a
            // soft-deleted participant makes this return null (the notification is skipped).
            // Service.Business is loaded to resolve the business language for the email.
            => Set
                .AsNoTracking()
                .Include(w => w.Client)
                .Include(w => w.Service)
                    .ThenInclude(s => s.Business)
                .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        /// <inheritdoc />
        public Task<WaitlistEntry?> GetNextWaitingForSlotAsync(
            int businessId, int serviceId, DateOnly date, TimeOnly startTime, int employeeId,
            CancellationToken cancellationToken = default)
            // IgnoreQueryFilters + explicit liveness: never notify a client/service
            // that was soft-deleted (BIZ-03). Tracked so the caller marks it Notified.
            => Set
                .IgnoreQueryFilters()
                .Where(w =>
                    w.Status == WaitlistStatus.Waiting
                    && w.BusinessId == businessId
                    && w.ServiceId == serviceId
                    && w.Date == date
                    && w.StartTime == startTime
                    && (w.EmployeeId == null || w.EmployeeId == employeeId)
                    && !w.Client.IsDeleted
                    && !w.Service.IsDeleted)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<WaitlistEntry>> GetActiveByClientAsync(int clientId, CancellationToken cancellationToken = default)
            => await Set
                .AsNoTracking()
                .Where(w => w.ClientId == clientId && w.Status != WaitlistStatus.Cancelled)
                .OrderBy(w => w.Date)
                .ThenBy(w => w.StartTime)
                .ToListAsync(cancellationToken);
    }
}
