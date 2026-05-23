using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Exceptions;

namespace MRC.Agendia.Infrastructure.Authorization
{
    /// <summary>
    /// Implementation of the resource-based authorization rules. Throws
    /// UnauthorizedAccessException when the user cannot operate on the
    /// requested resource.
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

        public async Task EnsureCanManageBusinessAsync(int businessId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId, cancellationToken);

            if (!isOwner)
                throw new UnauthorizedAccessException("No tienes permiso para gestionar este negocio.");
        }

        public async Task EnsureCanManageBusinessResourcesAsync(int businessId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            // Business owner?
            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId, cancellationToken);
            if (isOwner) return;

            // Business employee?
            var isEmployee = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == businessId && e.UserId == userId && e.IsActive, cancellationToken);
            if (isEmployee) return;

            throw new UnauthorizedAccessException("No tienes permiso para gestionar recursos de este negocio.");
        }

        // ---------- EMPLOYEE ----------

        public async Task EnsureCanViewEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.UserId, e.BusinessId })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EmployeeNotFoundException(employeeId);

            // The employee themselves
            if (employee.UserId == userId) return;

            // Owner of the employee's business
            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == employee.BusinessId && b.OwnerUserId == userId, cancellationToken);
            if (isOwner) return;

            throw new UnauthorizedAccessException("No tienes permiso para ver este empleado.");
        }

        public async Task EnsureCanUpdateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            // Same rules as view: admin, owner, or the employee themselves
            await EnsureCanViewEmployeeAsync(employeeId, cancellationToken);
        }

        public async Task EnsureCanDeleteEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var businessId = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => (int?)e.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new EmployeeNotFoundException(employeeId);

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId.Value && b.OwnerUserId == userId, cancellationToken);

            if (!isOwner)
                throw new UnauthorizedAccessException("Solo el dueno del negocio (o un admin) puede eliminar empleados.");
        }

        // ---------- CLIENT ----------

        public async Task EnsureCanManageClientAsync(int clientId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var isOwnClient = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c => c.Id == clientId && c.UserId == userId, cancellationToken);

            if (!isOwnClient)
                throw new UnauthorizedAccessException("Solo puedes gestionar tu propia cuenta de cliente.");
        }

        // ---------- APPOINTMENT ----------

        public async Task EnsureCanManageAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default)
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
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new AppointmentNotFoundException(appointmentId);

            // Business owner
            if (appointment.OwnerUserId == userId) return;

            // Employee of the business (any, not only the appointment's)
            var isEmployeeOfBusiness = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == appointment.BusinessId && e.UserId == userId && e.IsActive, cancellationToken);
            if (isEmployeeOfBusiness) return;

            // The appointment's client
            if (appointment.ClientUserId == userId) return;

            throw new UnauthorizedAccessException("No tienes permiso para gestionar esta cita.");
        }

        public async Task EnsureCanCreateAppointmentAsync(int clientId, int employeeId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            // Target employee and their business
            var employee = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.BusinessId, BusinessOwnerUserId = e.Business.OwnerUserId })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new EmployeeNotFoundException(employeeId);

            // Owner of the employee's business
            if (employee.BusinessOwnerUserId == userId) return;

            // Employee of the same business
            var isEmployeeOfBusiness = await _context.Employees
                .AsNoTracking()
                .AnyAsync(e => e.BusinessId == employee.BusinessId && e.UserId == userId && e.IsActive, cancellationToken);
            if (isEmployeeOfBusiness) return;

            // If Client, can only create an appointment for themselves
            if (_currentUser.IsInRole(Roles.Client))
            {
                var clientUserId = await _context.Clients
                    .AsNoTracking()
                    .Where(c => c.Id == clientId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (clientUserId == userId) return;

                throw new UnauthorizedAccessException("Solo puedes crear citas para tu propia cuenta de cliente.");
            }

            throw new UnauthorizedAccessException("No tienes permiso para crear esta cita.");
        }

        // ---------- BUSINESS-SCOPED RESOURCES (with id lookup) ----------

        public async Task EnsureCanManageServiceAsync(int serviceId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.Services
                .AsNoTracking()
                .Where(s => s.Id == serviceId)
                .Select(s => (int?)s.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ServiceNotFoundException(serviceId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }

        public async Task EnsureCanManageScheduleTemplateAsync(int templateId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.ScheduleTemplates
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => (int?)t.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ScheduleTemplateNotFoundException(templateId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }

        public async Task EnsureCanManageScheduleOverrideAsync(int overrideId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.ScheduleOverrides
                .AsNoTracking()
                .Where(o => o.Id == overrideId)
                .Select(o => (int?)o.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ScheduleOverrideNotFoundException(overrideId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }
    }
}
