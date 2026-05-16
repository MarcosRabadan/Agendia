using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Infrastructure.Authorization
{
    /// <summary>
    /// Implementacion de las reglas de autorizacion basadas en recursos.
    /// Lanza UnauthorizedAccessException si el usuario no puede operar
    /// sobre el recurso solicitado.
    /// </summary>
    public class ResourceAuthorizationService : IResourceAuthorizationService
    {
        private readonly AgendiaDbContext _context;
        private readonly ICurrentUserContext _currentUser;

        public ResourceAuthorizationService(AgendiaDbContext context, ICurrentUserContext currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        private string RequireUserId()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            return _currentUser.UserId!;
        }

        // ---------- BUSINESS ----------

        public async Task EnsureCanManageBusinessAsync(int businessId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId);

            if (!isOwner)
                throw new UnauthorizedAccessException("No tienes permiso para gestionar este negocio.");
        }

        public async Task EnsureCanManageBusinessResourcesAsync(int businessId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            // Owner del negocio?
            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId);
            if (isOwner) return;

            // Empleado del negocio?
            var isEmployee = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == businessId && e.UserId == userId && e.IsActive);
            if (isEmployee) return;

            throw new UnauthorizedAccessException("No tienes permiso para gestionar recursos de este negocio.");
        }

        // ---------- EMPLOYEE ----------

        public async Task EnsureCanViewEmployeeAsync(int employeeId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.UserId, e.BusinessId })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Employee {employeeId} not found.");

            // El propio empleado
            if (employee.UserId == userId) return;

            // Dueno del negocio del empleado
            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == employee.BusinessId && b.OwnerUserId == userId);
            if (isOwner) return;

            throw new UnauthorizedAccessException("No tienes permiso para ver este empleado.");
        }

        public async Task EnsureCanUpdateEmployeeAsync(int employeeId)
        {
            // Mismas reglas que ver: admin, dueno, o el propio empleado
            await EnsureCanViewEmployeeAsync(employeeId);
        }

        public async Task EnsureCanDeleteEmployeeAsync(int employeeId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var businessId = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => (int?)e.BusinessId)
                .FirstOrDefaultAsync();

            if (businessId is null)
                throw new KeyNotFoundException($"Employee {employeeId} not found.");

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId.Value && b.OwnerUserId == userId);

            if (!isOwner)
                throw new UnauthorizedAccessException("Solo el dueno del negocio (o un admin) puede eliminar empleados.");
        }

        // ---------- CLIENT ----------

        public async Task EnsureCanManageClientAsync(int clientId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var isOwnClient = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.UserId == userId);

            if (!isOwnClient)
                throw new UnauthorizedAccessException("Solo puedes gestionar tu propia cuenta de cliente.");
        }

        // ---------- APPOINTMENT ----------

        public async Task EnsureCanManageAppointmentAsync(int appointmentId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.Id == appointmentId)
                .Select(a => new
                {
                    a.ClientId,
                    a.EmployeeId,
                    BusinessId = a.Employee.BusinessId,
                    OwnerUserId = a.Employee.Business.OwnerUserId,
                    ClientUserId = a.Client.UserId,
                    EmployeeUserId = a.Employee.UserId
                })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Appointment {appointmentId} not found.");

            // Owner del negocio
            if (appointment.OwnerUserId == userId) return;

            // Empleado del negocio (cualquiera, no solo el de la cita)
            var isEmployeeOfBusiness = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == appointment.BusinessId && e.UserId == userId && e.IsActive);
            if (isEmployeeOfBusiness) return;

            // Cliente de la cita
            if (appointment.ClientUserId == userId) return;

            throw new UnauthorizedAccessException("No tienes permiso para gestionar esta cita.");
        }

        public async Task EnsureCanCreateAppointmentAsync(int clientId, int employeeId)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            // Empleado destino y su negocio
            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.BusinessId, BusinessOwnerUserId = e.Business.OwnerUserId })
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException($"Employee {employeeId} not found.");

            // Owner del negocio del empleado
            if (employee.BusinessOwnerUserId == userId) return;

            // Empleado del mismo negocio
            var isEmployeeOfBusiness = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == employee.BusinessId && e.UserId == userId && e.IsActive);
            if (isEmployeeOfBusiness) return;

            // Si es Client, solo puede crear cita para si mismo
            if (_currentUser.IsInRole(Roles.Client))
            {
                var clientUserId = await _context.Clients
                    .AsNoTracking()
                    .Where(c => c.Id == clientId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();

                if (clientUserId == userId) return;

                throw new UnauthorizedAccessException("Solo puedes crear citas para tu propia cuenta de cliente.");
            }

            throw new UnauthorizedAccessException("No tienes permiso para crear esta cita.");
        }

        // ---------- BUSINESS-SCOPED RESOURCES (with id lookup) ----------

        public async Task EnsureCanManageServiceAsync(int serviceId)
        {
            var businessId = await _context.Services
                .AsNoTracking()
                .Where(s => s.Id == serviceId)
                .Select(s => (int?)s.BusinessId)
                .FirstOrDefaultAsync();

            if (businessId is null)
                throw new KeyNotFoundException($"Service {serviceId} not found.");

            await EnsureCanManageBusinessResourcesAsync(businessId.Value);
        }

        public async Task EnsureCanManageScheduleTemplateAsync(int templateId)
        {
            var businessId = await _context.ScheduleTemplates
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => (int?)t.BusinessId)
                .FirstOrDefaultAsync();

            if (businessId is null)
                throw new KeyNotFoundException($"ScheduleTemplate {templateId} not found.");

            await EnsureCanManageBusinessResourcesAsync(businessId.Value);
        }

        public async Task EnsureCanManageScheduleOverrideAsync(int overrideId)
        {
            var businessId = await _context.ScheduleOverrides
                .AsNoTracking()
                .Where(o => o.Id == overrideId)
                .Select(o => (int?)o.BusinessId)
                .FirstOrDefaultAsync();

            if (businessId is null)
                throw new KeyNotFoundException($"ScheduleOverride {overrideId} not found.");

            await EnsureCanManageBusinessResourcesAsync(businessId.Value);
        }
    }
}
