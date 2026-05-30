using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        /// <summary>Gets a tracked appointment by id, honouring the soft-delete filter.</summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointment, or null when it is soft-deleted or missing.</returns>
        Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets an appointment by id ignoring the soft-delete filter (for restore).</summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointment even if soft-deleted, or null when missing.</returns>
        Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an appointment by id with its client, service, extra services and
        /// employee (plus the employee's business) loaded, for read-to-DTO. Untracked.
        /// Ignores ALL soft-delete filters, including the appointment's OWN: the
        /// waitlist-on-delete flow looks the appointment up AFTER it has been
        /// soft-deleted to learn which slot was freed, so a soft-deleted appointment
        /// (or one with a soft-deleted parent) is still returned by design. Do NOT add
        /// a !IsDeleted filter here or that notification path would receive null.
        /// </summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointment with details, or null only when no row has that id.</returns>
        Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Live appointment (honours the soft-delete filter) with its extra
        /// services loaded, for read-to-DTO. Returns null for a soft-deleted or
        /// missing appointment.
        /// </summary>
        /// <param name="id">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The appointment with its extra services, or null when soft-deleted or missing.</returns>
        Task<Appointment?> GetByIdWithExtrasAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of live appointments with their extra services, newest start date first.</summary>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of appointments and the total count.</returns>
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Gets a page of a client's live appointments with their extra services, newest start date first.</summary>
        /// <param name="clientId">Client id.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Page size.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The page of the client's appointments and the total count.</returns>
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId,
                                                                                         int page,
                                                                                         int pageSize,
                                                                                         CancellationToken cancellationToken = default);

        /// <summary>Adds a new appointment to the context.</summary>
        /// <param name="appointment">The appointment to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

        /// <summary>Marks an appointment as modified.</summary>
        /// <param name="appointment">The appointment to update.</param>
        void Update(Appointment appointment);

        /// <summary>Removes an appointment (soft-deleted by the save interceptor).</summary>
        /// <param name="appointment">The appointment to delete.</param>
        void Delete(Appointment appointment);

        /// <summary>
        /// Gets the live appointments of a business that overlap [startDate, endDate),
        /// with their extra services loaded. Untracked; uses overlap (not containment)
        /// so bookings crossing the range edges are not lost.
        /// </summary>
        /// <param name="businessId">Business id (resolved through the employee).</param>
        /// <param name="startDate">Range start (inclusive on overlap).</param>
        /// <param name="endDate">Range end (exclusive on overlap).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The overlapping appointments.</returns>
        Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(int businessId,
                                                                        DateTime startDate,
                                                                        DateTime endDate,
                                                                        CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts an employee's capacity-occupying (Pending/Confirmed) appointments that
        /// overlap [startDate, endDate), for the conflict/capacity check. Ignores the
        /// soft-delete filter so a live appointment with a soft-deleted parent still counts.
        /// </summary>
        /// <param name="employeeId">Employee id.</param>
        /// <param name="startDate">Overlap window start (exclusive on end).</param>
        /// <param name="endDate">Overlap window end (exclusive on start).</param>
        /// <param name="excludeAppointmentId">Appointment id to exclude (the one being rescheduled), or null.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The number of overlapping occupying appointments.</returns>
        Task<int> CountOverlappingForEmployeeAsync(int employeeId,
                                                   DateTime startDate,
                                                   DateTime endDate,
                                                   int? excludeAppointmentId,
                                                   CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracked appointments of a recurring series (excluding soft-deleted),
        /// ordered by StartDate. Tracked because callers mutate them (cancel/move).
        /// </summary>
        /// <param name="seriesId">Series identifier shared by the occurrences.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The series' live appointments ordered by start date.</returns>
        Task<IReadOnlyList<Appointment>> GetBySeriesIdAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upcoming, capacity-occupying appointments of a business (optionally a
        /// single employee) in [fromInclusive, toExclusive), excluding soft-deleted
        /// participants and inactive employees. Used by the delay-notification flow.
        /// </summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="employeeId">Employee id to restrict to, or null for the whole business.</param>
        /// <param name="fromInclusive">Window start (inclusive).</param>
        /// <param name="toExclusive">Window end (exclusive).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching upcoming appointments ordered by start date.</returns>
        Task<IReadOnlyList<Appointment>> GetUpcomingForDelayAsync(int businessId,
                                                                  int? employeeId,
                                                                  DateTime fromInclusive,
                                                                  DateTime toExclusive,
                                                                  CancellationToken cancellationToken = default);

        /// <summary>
        /// Service ids of the appointment's extra services (beyond the primary
        /// ServiceId), or empty when it has none. Used to re-validate the total
        /// duration on reschedule.
        /// </summary>
        /// <param name="appointmentId">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The extra service ids, or an empty list when there are none.</returns>
        Task<IReadOnlyList<int>> GetExtraServiceIdsAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The owning business's self-service cancellation window in hours for the
        /// given appointment, or null when the appointment is gone or no window is
        /// configured. Used to enforce the self-service cancel/reschedule policy.
        /// </summary>
        /// <param name="appointmentId">Appointment id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The cancellation window in hours, or null when missing or not configured.</returns>
        Task<int?> GetCancellationWindowHoursAsync(int appointmentId, CancellationToken cancellationToken = default);
    }
}
