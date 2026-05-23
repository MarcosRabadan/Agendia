using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
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

        public Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            // IgnoreQueryFilters so the appointment still loads its related
            // Client/Employee/Service/Business even when one of them was
            // soft-deleted afterwards. Otherwise the soft-delete query filter
            // applies to the Includes and the required navigations come back null,
            // breaking notifications. Appointments keep their history (no cascade).
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
                .Include(a => a.Client)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId, int page, int pageSize, CancellationToken cancellationToken = default)
            => Set
                .AsNoTracking()
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var appointments = await Set
                 .Include(a => a.Employee)
                 .Where(a => a.Employee.BusinessId == businessId &&
                             a.StartDate >= startDate &&
                             a.EndDate <= endDate)
                 .ToListAsync(cancellationToken);
            return appointments;
        }
    }
}
