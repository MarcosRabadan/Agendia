using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments
{
    public interface IAppointmentService
    {
        /// <summary>Returns a page of all appointments.</summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The requested page of appointments.</returns>
        Task<PagedResult<AppointmentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Returns a page of the appointments belonging to the client identified by their user id; an empty page if the user has no client row.</summary>
        /// <param name="userId">Identity user id of the client.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The requested page of the client's appointments.</returns>
        Task<PagedResult<AppointmentDto>> GetPagedByClientUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets an appointment (with its extra services) by id.</summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointment, or null if it does not exist.</returns>
        Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates an appointment after validating and reserving the slot under a per-employee/day lock, then sends a best-effort confirmation email.</summary>
        /// <param name="dto">Data of the appointment to create.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created appointment.</returns>
        Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an appointment (reschedule, status or notes), enforcing terminal-state, role and self-service cancellation-window rules, and triggering cancellation/waitlist notifications when applicable.</summary>
        /// <param name="dto">Updated appointment data.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated appointment.</returns>
        Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes an appointment (honouring the self-service cancellation window for clients) and notifies the waitlist if it was occupying a slot.</summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when the appointment was deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Restores a previously soft-deleted appointment; idempotent if it is not deleted.</summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when the operation completes.</returns>
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Returns the appointments of a business that overlap the given date range.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="startDate">Inclusive start of the range.</param>
        /// <param name="endDate">Exclusive end of the range.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointments overlapping the range.</returns>
        Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
