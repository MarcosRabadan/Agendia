using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules
{
    /// <summary>
    /// Bulk "initial setup wizard" for a business schedule: creates the yearly
    /// templates and resolves the override calendar (holidays, vacations, ad-hoc
    /// closures). Split out of <see cref="IScheduleService"/> (which keeps the
    /// CRUD and effective-schedule queries) so each has a single responsibility.
    /// </summary>
    public interface IScheduleGenerationService
    {
        /// <summary>Builds and persists the yearly templates and override calendar (holidays, vacations, closures) for a business.</summary>
        /// <param name="dto">Generation request: year, templates and the holidays/vacations/closures to apply.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created template ids and a summary of working/holiday/vacation/closed days, with any warnings.</returns>
        Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Same input and validations as <see cref="GenerateScheduleAsync"/> but
        /// persists nothing: builds the templates/overrides in memory, merges
        /// them with the business's existing schedule and returns the resulting
        /// calendar for the whole year so the front can preview the result.
        /// </summary>
        /// <param name="dto">Generation request to preview.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The resulting calendar for the whole year, without persisting anything.</returns>
        Task<IEnumerable<CalendarDayDto>> PreviewScheduleAsync(GenerateScheduleRequestDto dto, CancellationToken cancellationToken = default);
    }
}
