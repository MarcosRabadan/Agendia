using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Appointment?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
        Task<Appointment?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Live appointment (honours the soft-delete filter) with its extra
        /// services loaded, for read-to-DTO. Returns null for a soft-deleted or
        /// missing appointment.
        /// </summary>
        Task<Appointment?> GetByIdWithExtrasAsync(int id, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
        void Update(Appointment appointment);
        void Delete(Appointment appointment);
        Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(
            int businessId,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default);
        Task<int> CountOverlappingForEmployeeAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate,
            int? excludeAppointmentId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracked appointments of a recurring series (excluding soft-deleted),
        /// ordered by StartDate. Tracked because callers mutate them (cancel/move).
        /// </summary>
        Task<IReadOnlyList<Appointment>> GetBySeriesIdAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upcoming, capacity-occupying appointments of a business (optionally a
        /// single employee) in [fromInclusive, toExclusive), excluding soft-deleted
        /// participants and inactive employees. Used by the delay-notification flow.
        /// </summary>
        Task<IReadOnlyList<Appointment>> GetUpcomingForDelayAsync(
            int businessId,
            int? employeeId,
            DateTime fromInclusive,
            DateTime toExclusive,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Service ids of the appointment's extra services (beyond the primary
        /// ServiceId), or empty when it has none. Used to re-validate the total
        /// duration on reschedule.
        /// </summary>
        Task<IReadOnlyList<int>> GetExtraServiceIdsAsync(int appointmentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// The owning business's self-service cancellation window in hours for the
        /// given appointment, or null when the appointment is gone or no window is
        /// configured. Used to enforce the self-service cancel/reschedule policy.
        /// </summary>
        Task<int?> GetCancellationWindowHoursAsync(int appointmentId, CancellationToken cancellationToken = default);
    }
}
