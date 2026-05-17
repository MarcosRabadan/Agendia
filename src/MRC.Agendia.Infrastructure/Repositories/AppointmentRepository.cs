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

        public async Task<Appointment?> GetByIdAsync(int id)
            => await _context.Appointments.FindAsync(id);

        public async Task<IEnumerable<Appointment>> GetAllAsync()
            => await _context.Appointments.ToListAsync();

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

        public async Task AddAsync(Appointment appointment)
            => await _context.Appointments.AddAsync(appointment);

        public void Update(Appointment appointment)
            => _context.Appointments.Update(appointment);

        public void Delete(Appointment appointment)
            => _context.Appointments.Remove(appointment);

        public async Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate)
        {
           var appointments = await _context.Appointments
                .Include(a => a.Employee)
                .Where(a => a.Employee.BusinessId == businessId &&
                            a.StartDate >= startDate &&
                            a.EndDate <= endDate)
                .ToListAsync();
            return appointments;
        }
    }
}
