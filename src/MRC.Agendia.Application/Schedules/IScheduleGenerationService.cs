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
        Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto);

        /// <summary>
        /// Same input and validations as <see cref="GenerateScheduleAsync"/> but
        /// persists nothing: builds the templates/overrides in memory, merges
        /// them with the business's existing schedule and returns the resulting
        /// calendar for the whole year so the front can show "así te quedará".
        /// </summary>
        Task<IEnumerable<CalendarDayDto>> PreviewScheduleAsync(GenerateScheduleRequestDto dto);
    }
}
