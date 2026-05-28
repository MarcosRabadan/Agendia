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
        Task<AppointmentSeriesResultDto> CreateSeriesAsync(CreateAppointmentSeriesDto dto, CancellationToken cancellationToken = default);

        /// <summary>Cancels the future, still-active occurrences of the series.</summary>
        Task<AppointmentSeriesCountResultDto> CancelSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Soft-deletes every (non-deleted) appointment of the series.</summary>
        Task<AppointmentSeriesCountResultDto> DeleteSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);

        /// <summary>Shifts the future occurrences of the series; conflicting ones are skipped.</summary>
        Task<MoveAppointmentSeriesResultDto> MoveSeriesAsync(Guid seriesId, MoveAppointmentSeriesDto dto, CancellationToken cancellationToken = default);
    }
}
