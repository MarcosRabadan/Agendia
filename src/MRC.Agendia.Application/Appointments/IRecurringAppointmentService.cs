using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Bulk operations over a recurring appointment series (e.g. "every Friday at
    /// 16h"). Generation reuses the single-appointment scheduling validator and the
    /// per-employee/day booking guard, so a series can never overbook a slot or
    /// place an appointment on a closed day; occurrences that do not fit are
    /// skipped and reported rather than aborting the whole request.
    /// </summary>
    public interface IRecurringAppointmentService
    {
        /// <summary>Materializes a recurring series as individual appointments; occurrences that do not fit are skipped and reported.</summary>
        /// <param name="dto">Recurrence definition (frequency, interval, days, time window) plus the appointment details.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The series id, the created appointments and the skipped occurrences with their reason.</returns>
        Task<AppointmentSeriesResultDto> CreateSeriesAsync(CreateAppointmentSeriesDto dto, CancellationToken cancellationToken = default);

        /// <summary>Cancels the future, still-active occurrences of the series.</summary>
        /// <param name="seriesId">Id of the series.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The series id and how many occurrences were cancelled.</returns>
        Task<AppointmentSeriesCountResultDto> CancelSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes every (non-deleted) appointment of the series.</summary>
        /// <param name="seriesId">Id of the series.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The series id and how many occurrences were deleted.</returns>
        Task<AppointmentSeriesCountResultDto> DeleteSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Shifts the future occurrences of the series; conflicting ones are skipped.</summary>
        /// <param name="seriesId">Id of the series.</param>
        /// <param name="dto">Day shift and optional new start time to apply.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The series id, the moved appointments and the skipped occurrences with their reason.</returns>
        Task<MoveAppointmentSeriesResultDto> MoveSeriesAsync(Guid seriesId, MoveAppointmentSeriesDto dto, CancellationToken cancellationToken = default);
    }
}
