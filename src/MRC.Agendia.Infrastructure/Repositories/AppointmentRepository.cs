using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Infrastructure.Repositories
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly AgendiaDbContext _context;

        public AppointmentRepository(AgendiaDbContext context)
        {
            _context = context;
        }

        public async Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await _context.Appointments.FindAsync(new object?[] { id }, cancellationToken);

        public Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
            => _context.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Business)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        public async Task<IEnumerable<Appointment>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _context.Appointments.ToListAsync(cancellationToken);

        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Appointments
                .AsNoTracking()
                .Include(a => a.Client)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId, int page, int pageSize, CancellationToken cancellationToken = default)
            => _context.Appointments
                .AsNoTracking()
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Where(a => a.ClientId == clientId)
                .OrderByDescending(a => a.StartDate)
                .ToPagedListAsync(page, pageSize, cancellationToken);

        public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
            => await _context.Appointments.AddAsync(appointment, cancellationToken);

        public void Update(Appointment appointment)
            => _context.Appointments.Update(appointment);

        public void Delete(Appointment appointment)
            => _context.Appointments.Remove(appointment);

        public async Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var appointments = await _context.Appointments
                 .Include(a => a.Employee)
                 .Where(a => a.Employee.BusinessId == businessId &&
                             a.StartDate >= startDate &&
                             a.EndDate <= endDate)
                 .ToListAsync(cancellationToken);
            return appointments;
        }
    }
}
