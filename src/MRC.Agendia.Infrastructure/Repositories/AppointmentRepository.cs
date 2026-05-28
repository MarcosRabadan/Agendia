using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class AppointmentRepository : RepositoryBase<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(AgendiaDbContext context) : base(context)
        {
        }

        public Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        // The reads below IgnoreQueryFilters and re-apply !a.IsDeleted explicitly:
        // Client/Employee/Service are required navigations with a soft-delete filter,
        // so an Include without IgnoreQueryFilters turns into an INNER JOIN that drops
        // any appointment whose parent was soft-deleted. That would hide live bookings
        // from listings AND from the capacity/conflict count (enabling double-booking).
        // Appointments keep their own history; only soft-deleted appointments are hidden.

        public Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Business)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId, int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => a.ClientId == clientId && !a.IsDeleted)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var appointments = await Set
                 .AsNoTracking()
                 .IgnoreQueryFilters()
                 .Where(a => !a.IsDeleted &&
                             a.Employee.BusinessId == businessId &&
                             a.StartDate < endDate &&
                             a.EndDate > startDate)
                 .ToListAsync(cancellationToken);
            return appointments;
        }

        public async Task<IReadOnlyList<Appointment>> GetBySeriesIdAsync(Guid seriesId, CancellationToken cancellationToken = default)
            // Tracked (no AsNoTracking) because cancel/move/delete mutate the rows.
            // IgnoreQueryFilters + !IsDeleted keeps live appointments whose parent
            // was soft-deleted (consistent with the other reads above).
            => await Set
                .IgnoreQueryFilters()
                .Where(a => a.SeriesId == seriesId && !a.IsDeleted)
                .OrderBy(a => a.StartDate)
                .ToListAsync(cancellationToken);

        public Task<int> CountOverlappingForEmployeeAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            int? excludeAppointmentId,
            CancellationToken cancellationToken = default)
            // Mirror of AppointmentStatus.OccupiesCapacity() (Pending|Confirmed),
            // inlined because the extension method cannot be translated to SQL.
            // IgnoreQueryFilters + !IsDeleted so a live appointment whose parent is
            // soft-deleted still counts against capacity (consistent with #133).
            => Set
                .IgnoreQueryFilters()
                .CountAsync(a => !a.IsDeleted
                    && a.EmployeeId == employeeId
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                    && (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value)
                    && a.StartDate < endDate
                    && a.EndDate > startDate,
                    cancellationToken);
    }
}
