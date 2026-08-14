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
        /// <param name="businessId">Id of the business to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, business owner, or business employee. For creating/editing/
        /// deleting business resources (services, templates, overrides, etc.).
        /// </summary>
        /// <param name="businessId">Id of the business that owns the resources.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageBusinessResourcesAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>Admin, owner of the employee's business, or the employee themselves.</summary>
        /// <param name="employeeId">Id of the employee to view.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanViewEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin, owner of the employee's business, or the employee themselves.</summary>
        /// <param name="employeeId">Id of the employee to update.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanUpdateEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner of the employee's business.</summary>
        /// <param name="employeeId">Id of the employee to delete.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanDeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, owner/employee of the appointment's business, or the
        /// appointment's client.
        /// </summary>
        /// <param name="appointmentId">Id of the appointment to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// When creating an appointment:
        /// - Admin: always allowed.
        /// - Owner/Employee of the appointment employee's business: allowed.
        /// - Client: only if the appointment is for their own user id.
        /// </summary>
        /// <param name="clientUserId">Harmony user id of the client the appointment is for.</param>
        /// <param name="employeeId">Id of the employee the appointment is assigned to.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanCreateAppointmentAsync(string clientUserId, Guid employeeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin or owner/employee of the business that owns the series. Resolves
        /// the business from any appointment of the series.
        /// </summary>
        /// <param name="seriesId">Id of the appointment series to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageAppointmentSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the service's business.</summary>
        /// <param name="serviceId">Id of the service to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the template's business.</summary>
        /// <param name="templateId">Id of the schedule template to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageScheduleTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

        /// <summary>Admin or owner/employee of the override's business.</summary>
        /// <param name="overrideId">Id of the schedule override to manage.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task EnsureCanManageScheduleOverrideAsync(Guid overrideId, CancellationToken cancellationToken = default);
    }
}
