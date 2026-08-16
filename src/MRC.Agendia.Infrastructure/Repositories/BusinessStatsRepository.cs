using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Enums;
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
        public async Task<IReadOnlyList<AppointmentStatsRow>> GetAppointmentsAsync(Guid businessId,
                                                                                   DateTime fromInclusive,
                                                                                   DateTime toExclusive,
                                                                                   CancellationToken cancellationToken = default)
            // Server-side filter + projection: only the columns the aggregation needs.
            // IgnoreQueryFilters + explicit !IsDeleted keeps the (historical) appointment
            // even if its service was soft-deleted later, so usage counts stay accurate;
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
                    a.ServiceId))
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<AppointmentStatus>> GetClientAppointmentStatusesAsync(Guid businessId,
                                                                                              string clientUserId,
                                                                                              DateTime fromInclusive,
                                                                                              DateTime toExclusive,
                                                                                              CancellationToken cancellationToken = default)
            // Same projection discipline as above: the outcome is the only column the
            // reliability metrics need, so nothing else leaves the database.
            => await _context.Appointments
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted
                    && a.Employee.BusinessId == businessId
                    && a.ClientUserId == clientUserId
                    && a.StartDate >= fromInclusive
                    && a.StartDate < toExclusive)
                .Select(a => a.Status)
                .ToListAsync(cancellationToken);
    }
}
