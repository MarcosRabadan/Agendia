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

        /// <inheritdoc />
        public Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        // Most reads below IgnoreQueryFilters and re-apply !a.IsDeleted explicitly:
        // Client/Employee/Service are required navigations with a soft-delete filter,
        // so an Include without IgnoreQueryFilters turns into an INNER JOIN that drops
        // any appointment whose parent was soft-deleted. That would hide live bookings
        // from listings AND from the capacity/conflict count (enabling double-booking).
        // Appointments keep their own history; soft-deleted appointments are hidden.
        // EXCEPTION: GetByIdWithDetailsAsync intentionally does NOT re-apply !a.IsDeleted
        // (see its interface remarks) so the waitlist-on-delete flow can read a freed
        // appointment back right after it has been soft-deleted.

        /// <inheritdoc />
        public Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Include(a => a.ExtraServices)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Business)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        /// <inheritdoc />
        public Task<Appointment?> GetByIdWithExtrasAsync(int id, CancellationToken cancellationToken = default)
            // Only ExtraServices is included (no soft-deletable parent navigation),
            // so the global !IsDeleted filter on Appointment applies as wanted: a
            // soft-deleted appointment is not returned. AsNoTracking: read-only.
            => Set
                .AsNoTracking()
                .Include(a => a.ExtraServices)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(a => a.ExtraServices)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        /// <inheritdoc />
        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId,
                                                                                                int page,
                                                                                                int pageSize,
                                                                                                CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(a => a.ExtraServices)
                .Where(a => a.ClientId == clientId && !a.IsDeleted)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        /// <inheritdoc />
        public async Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId,
                                                                                     DateTime startDate,
                                                                                     DateTime endDate,
                                                                                     CancellationToken cancellationToken = default)
        {
            var appointments = await Set
                 .AsNoTracking()
                 .IgnoreQueryFilters()
                 .Include(a => a.ExtraServices)
                 .Where(a => !a.IsDeleted &&
                             a.Employee.BusinessId == businessId &&
                             a.StartDate < endDate &&
                             a.EndDate > startDate)
                 .ToListAsync(cancellationToken);
            return appointments;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Appointment>> GetBySeriesIdAsync(Guid seriesId, CancellationToken cancellationToken = default)
            // Tracked (no AsNoTracking) because cancel/move/delete mutate the rows.
            // IgnoreQueryFilters + !IsDeleted keeps live appointments whose parent
            // was soft-deleted (consistent with the other reads above).
            => await Set
                .IgnoreQueryFilters()
                .Where(a => a.SeriesId == seriesId && !a.IsDeleted)
                .OrderBy(a => a.StartDate)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public Task<int> CountOverlappingForEmployeeAsync(int employeeId,
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

        /// <inheritdoc />
        public async Task<IReadOnlyList<Appointment>> GetUpcomingForDelayAsync(int businessId,
                                                                               int? employeeId,
                                                                               DateTime fromInclusive,
                                                                               DateTime toExclusive,
                                                                               CancellationToken cancellationToken = default)
            // IgnoreQueryFilters + explicit liveness checks: only notify clients of
            // live appointments whose client/employee/business are not soft-deleted
            // and whose employee is active (BIZ-03). AsNoTracking: read-only.
            => await Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted
                    && a.Employee.BusinessId == businessId
                    && (employeeId == null || a.EmployeeId == employeeId)
                    && a.StartDate >= fromInclusive
                    && a.StartDate < toExclusive
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
                    && !a.Client.IsDeleted
                    && !a.Employee.IsDeleted
                    && a.Employee.IsActive
                    && !a.Employee.Business.IsDeleted)
                .OrderBy(a => a.StartDate)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<int>> GetExtraServiceIdsAsync(int appointmentId, CancellationToken cancellationToken = default)
            // Service ids of the appointment's extra services, used to re-validate
            // the total duration on reschedule without loading the whole graph.
            // IgnoreQueryFilters mirrors the other appointment reads.
            => await Set
                .IgnoreQueryFilters()
                .Where(a => a.Id == appointmentId)
                .SelectMany(a => a.ExtraServices)
                .Select(e => e.ServiceId)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public Task<int?> GetCancellationWindowHoursAsync(int appointmentId, CancellationToken cancellationToken = default)
            // Project the owning business's window through employee -> business.
            // IgnoreQueryFilters so a soft-deleted participant does not turn the
            // required navigation into an INNER JOIN that drops the row (consistent
            // with the reads above); a missing row or a null window both surface as
            // null => no restriction.
            => Set
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => a.Id == appointmentId)
                .Select(a => a.Employee.Business.CancellationWindowHours)
                .FirstOrDefaultAsync(cancellationToken);
    }
}
