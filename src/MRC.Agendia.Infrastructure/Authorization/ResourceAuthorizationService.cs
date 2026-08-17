using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Entities;
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
        private readonly ICurrentBusinessScope _businessScope;

        public ResourceAuthorizationService(AgendiaDbContext context,
                                            ICurrentUserContext currentUser,
                                            ICurrentBusinessScope businessScope)
        {
            _context = context;
            _currentUser = currentUser;
            _businessScope = businessScope;
        }

        private string RequireUserId()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrEmpty(_currentUser.UserId))
                throw new UnauthorizedAccessException("User not authenticated.");
            return _currentUser.UserId!;
        }

        // ---------- BUSINESS ----------

        /// <inheritdoc />
        public async Task EnsureCanManageBusinessAsync(Guid businessId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId && b.OwnerUserId == userId, cancellationToken);

            if (!isOwner)
                throw new UnauthorizedAccessException("You do not have permission to manage this business.");
        }

        /// <inheritdoc />
        public async Task EnsureCanManageBusinessResourcesAsync(Guid businessId, CancellationToken cancellationToken = default)
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

            throw new UnauthorizedAccessException("You do not have permission to manage this business's resources.");
        }

        // ---------- EMPLOYEE ----------

        /// <inheritdoc />
        public async Task EnsureCanViewEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
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

            throw new UnauthorizedAccessException("You do not have permission to view this employee.");
        }

        /// <inheritdoc />
        public async Task EnsureCanUpdateEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            // Same rules as view: admin, owner, or the employee themselves
            await EnsureCanViewEmployeeAsync(employeeId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureCanDeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var businessId = await _context.Employees
                .AsNoTracking()
                .Where(e => e.Id == employeeId)
                .Select(e => (Guid?)e.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new EmployeeNotFoundException(employeeId);

            var isOwner = await _context.Businesses
                .AsNoTracking()
                .AnyAsync(b => b.Id == businessId.Value && b.OwnerUserId == userId, cancellationToken);

            if (!isOwner)
                throw new UnauthorizedAccessException("Only the business owner (or an admin) can delete employees.");
        }

        // ---------- APPOINTMENT ----------

        /// <summary>Appointments as resource authorization must see them.</summary>
        /// <remarks>
        /// Both appointment checks reach the owning business through the required
        /// <c>Appointment -> Employee -> Business</c> navigations, and both parents carry a
        /// soft-delete query filter. EF applies that filter to the INNER JOIN, which DROPS the
        /// appointment as soon as a participant is soft-deleted: the client of a live future
        /// booking was getting a 404 for their own appointment because the employee had left
        /// the business, and so was the owner (#292). An appointment keeps its history, so the
        /// filters come off here - and the two conditions that DO apply are re-stated, because
        /// <c>IgnoreQueryFilters</c> is all-or-nothing in EF 9:
        /// <list type="bullet">
        /// <item>the appointment itself must not be soft-deleted (a deleted one stays a 404);</item>
        /// <item>the caller's business scope still applies, so another tenant's appointment
        /// stays invisible as a 404 instead of becoming a 403 that would confirm it exists
        /// (the R7 convention).</item>
        /// </list>
        /// </remarks>
        private IQueryable<Appointment> AppointmentsForAuthorization()
            => _context.Appointments
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(a => !a.IsDeleted
                    && (!_businessScope.IsRestricted
                        || _businessScope.BusinessIds.Contains(a.Employee.BusinessId)));

        /// <inheritdoc />
        public async Task EnsureCanManageAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            var appointment = await AppointmentsForAuthorization()
                .Where(a => a.Id == appointmentId)
                .Select(a => new
                {
                    a.ClientUserId,
                    a.EmployeeId,
                    BusinessId = a.Employee.BusinessId,
                    OwnerUserId = a.Employee.Business.OwnerUserId,
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

            throw new UnauthorizedAccessException("You do not have permission to manage this appointment.");
        }

        /// <inheritdoc />
        public async Task EnsureCanCreateAppointmentAsync(string clientUserId, Guid employeeId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return;
            var userId = RequireUserId();

            // NOTE (R7): for an Owner/Employee caller the global business-scope filter already
            // restricts _context.Employees to their own business(es), so targeting an employee
            // of ANOTHER business surfaces EmployeeNotFoundException (404) here rather than 403.
            // It still denies correctly - only the status code differs. (Admin is unscoped and
            // returned above; a Client is handled explicitly at the end.)
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

            // A Client can only create an appointment for their own user id.
            if (_currentUser.IsInRole(Roles.Client))
            {
                if (clientUserId == userId) return;

                throw new UnauthorizedAccessException("You can only create appointments for your own client account.");
            }

            throw new UnauthorizedAccessException("You do not have permission to create this appointment.");
        }

        /// <inheritdoc />
        public async Task EnsureCanManageAppointmentSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default)
        {
            // Resolve the owning business from any (live) appointment of the series.
            // Doubles as an existence check: an empty series is a 404.
            var businessId = await AppointmentsForAuthorization()
                .Where(a => a.SeriesId == seriesId)
                .Select(a => (Guid?)a.Employee.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new AppointmentSeriesNotFoundException(seriesId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }

        // ---------- BUSINESS-SCOPED RESOURCES (with id lookup) ----------

        /// <inheritdoc />
        public async Task EnsureCanManageServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.Services
                .AsNoTracking()
                .Where(s => s.Id == serviceId)
                .Select(s => (Guid?)s.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ServiceNotFoundException(serviceId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureCanManageScheduleTemplateAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.ScheduleTemplates
                .AsNoTracking()
                .Where(t => t.Id == templateId)
                .Select(t => (Guid?)t.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ScheduleTemplateNotFoundException(templateId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureCanManageScheduleOverrideAsync(Guid overrideId, CancellationToken cancellationToken = default)
        {
            var businessId = await _context.ScheduleOverrides
                .AsNoTracking()
                .Where(o => o.Id == overrideId)
                .Select(o => (Guid?)o.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (businessId is null)
                throw new ScheduleOverrideNotFoundException(overrideId);

            await EnsureCanManageBusinessResourcesAsync(businessId.Value, cancellationToken);
        }
    }
}
