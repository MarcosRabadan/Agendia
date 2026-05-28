namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Resource-based authorization checks. Each Ensure* method throws
    /// UnauthorizedAccessException when the current user is not allowed to
    /// perform that action on that resource. The exception middleware turns it
    /// into a 403 Forbidden.
    /// </summary>
    public interface IResourceAuthorizationService
    {
        /// <summary>Admin or owner of this business.</summary>
        Task EnsureCanManageBusinessAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, business owner, or business employee. For creating/editing/
        /// deleting business resources (services, templates, overrides, etc.).
        /// </summary>
        Task EnsureCanManageBusinessResourcesAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>Admin, owner of the employee's business, or the employee themselves.</summary>
        Task EnsureCanViewEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin, owner of the employee's business, or the employee themselves.</summary>
        Task EnsureCanUpdateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner of the employee's business.</summary>
        Task EnsureCanDeleteEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin or the client themselves.</summary>
        Task EnsureCanManageClientAsync(int clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, owner/employee of the appointment's business, or the
        /// appointment's client.
        /// </summary>
        Task EnsureCanManageAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// When creating an appointment:
        /// - Admin: always allowed.
        /// - Owner/Employee of the appointment employee's business: allowed.
        /// - Client: only if the appointment is for their own Client.Id.
        /// </summary>
        Task EnsureCanCreateAppointmentAsync(int clientId, int employeeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin or owner/employee of the business that owns the series. Resolves
        /// the business from any appointment of the series.
        /// </summary>
        Task EnsureCanManageAppointmentSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the service's business.</summary>
        Task EnsureCanManageServiceAsync(int serviceId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the template's business.</summary>
        Task EnsureCanManageScheduleTemplateAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the override's business.</summary>
        Task EnsureCanManageScheduleOverrideAsync(int overrideId, CancellationToken cancellationToken = default);
    }
}
