using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Statistics;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class BusinessStatsRepository : IBusinessStatsRepository
    {
        private readonly AgendiaDbContext _context;

        public BusinessStatsRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<AppointmentStatsRow>> GetAppointmentsAsync(int businessId,
                                                                                   DateTime fromInclusive,
                                                                                   DateTime toExclusive,
                                                                                   CancellationToken cancellationToken = default)
            // Server-side filter + projection: only the columns the aggregation needs.
            // IgnoreQueryFilters + explicit !IsDeleted keeps the (historical) appointment
            // even if its service was soft-deleted later, so revenue/usage stay accurate;
            // only soft-deleted appointments themselves are excluded.
            => await _context.Appointments
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted
                    && a.Employee.BusinessId == businessId
                    && a.StartDate >= fromInclusive
                    && a.StartDate < toExclusive)
                .Select(a => new AppointmentStatsRow(
                    a.StartDate,
                    a.Status,
                    a.ServiceId,
                    a.Service.Name,
                    a.Service.Price))
                .ToListAsync(cancellationToken);
    }
}
