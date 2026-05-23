namespace MRC.Agendia.Application.Authorization
{
    /// <summary>
    /// Comprobaciones de autorizacion basadas en recursos (resource-based).
    /// Cada metodo Ensure* lanza UnauthorizedAccessException si el usuario
    /// actual no tiene permiso para esa accion sobre ese recurso.
    /// El middleware de errores convierte la excepcion en 403 Forbidden.
    /// </summary>
    public interface IResourceAuthorizationService
    {
        /// <summary>Admin o dueno de este negocio.</summary>
        Task EnsureCanManageBusinessAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, dueno del negocio, o empleado del negocio.
        /// Para crear/editar/borrar recursos del negocio
        /// (servicios, plantillas, overrides, etc.).
        /// </summary>
        Task EnsureCanManageBusinessResourcesAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>Admin, dueno del negocio del empleado, o el propio empleado.</summary>
        Task EnsureCanViewEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin, dueno del negocio del empleado, o el propio empleado.</summary>
        Task EnsureCanUpdateEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin o dueno del negocio del empleado.</summary>
        Task EnsureCanDeleteEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin o el propio cliente.</summary>
        Task EnsureCanManageClientAsync(int clientId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Admin, owner/empleado del negocio de la cita,
        /// o cliente de la cita.
        /// </summary>
        Task EnsureCanManageAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Al crear una cita:
        /// - Admin: siempre permitido.
        /// - Owner/Employee del negocio del empleado de la cita: permitido.
        /// - Client: solo si la cita es para su propio Client.Id.
        /// </summary>
        Task EnsureCanCreateAppointmentAsync(int clientId, int employeeId, CancellationToken cancellationToken = default);

        /// <summary>Admin o owner/empleado del negocio del servicio.</summary>
        Task EnsureCanManageServiceAsync(int serviceId, CancellationToken cancellationToken = default);

        /// <summary>Admin o owner/empleado del negocio de la plantilla.</summary>
        Task EnsureCanManageScheduleTemplateAsync(int templateId, CancellationToken cancellationToken = default);

        /// <summary>Admin o owner/empleado del negocio del override.</summary>
        Task EnsureCanManageScheduleOverrideAsync(int overrideId, CancellationToken cancellationToken = default);
    }
}
