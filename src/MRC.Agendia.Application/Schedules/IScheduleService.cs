using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules
{
    public interface IScheduleService
    {
        // Templates

        /// <summary>Returns the schedule templates of a business.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The business's schedule templates.</returns>
        Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);

        /// <summary>Gets a schedule template (with its weekly slots) by id.</summary>
        /// <param name="id">Template id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The template, or null if it does not exist.</returns>
        Task<ScheduleTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a schedule template, rejecting it if its date range overlaps an existing one.</summary>
        /// <param name="dto">Data of the template to create.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created template.</returns>
        Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates a schedule template and replaces its weekly slots, rejecting it if its date range overlaps another one.</summary>
        /// <param name="dto">Updated template data.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated template.</returns>
        Task<ScheduleTemplateDto> UpdateTemplateAsync(UpdateScheduleTemplateDto dto, CancellationToken cancellationToken = default);

        /// <summary>Deletes a schedule template.</summary>
        /// <param name="id">Template id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when the template was deleted.</returns>
        Task<bool> DeleteTemplateAsync(int id, CancellationToken cancellationToken = default);

        // Overrides

        /// <summary>Returns the schedule overrides of a business, optionally limited to a date range.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="from">Inclusive start of the range, or null for no lower bound.</param>
        /// <param name="to">Inclusive end of the range, or null for no upper bound.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The matching schedule overrides.</returns>
        Task<IEnumerable<ScheduleOverrideDto>> GetOverridesByBusinessIdAsync(int businessId,
                                                                             DateOnly? from,
                                                                             DateOnly? to,
                                                                             CancellationToken cancellationToken = default);

        /// <summary>Gets a schedule override (with its custom slots) by id.</summary>
        /// <param name="id">Override id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The override, or null if it does not exist.</returns>
        Task<ScheduleOverrideDto?> GetOverrideByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a schedule override, rejecting it if one already exists for that (business, date).</summary>
        /// <param name="dto">Data of the override to create.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created override.</returns>
        Task<ScheduleOverrideDto> CreateOverrideAsync(CreateScheduleOverrideDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates a schedule override and replaces its custom slots, rejecting a move onto a date already taken by another override.</summary>
        /// <param name="dto">Updated override data.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated override.</returns>
        Task<ScheduleOverrideDto> UpdateOverrideAsync(UpdateScheduleOverrideDto dto, CancellationToken cancellationToken = default);

        /// <summary>Deletes a schedule override.</summary>
        /// <param name="id">Override id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True when the override was deleted.</returns>
        Task<bool> DeleteOverrideAsync(int id, CancellationToken cancellationToken = default);

        // Effective Schedule

        /// <summary>Resolves the effective schedule of a business for a single day, including the active and available templates.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="date">Day to resolve.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The effective schedule for that day.</returns>
        Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);

        /// <summary>Resolves the effective schedule of a business for each day in a date range.</summary>
        /// <param name="businessId">Business id.</param>
        /// <param name="from">Inclusive start of the range.</param>
        /// <param name="to">Inclusive end of the range.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>One calendar day per date in the range.</returns>
        Task<IEnumerable<CalendarDayDto>> GetCalendarAsync(int businessId,
                                                           DateOnly from,
                                                           DateOnly to,
                                                           CancellationToken cancellationToken = default);
    }
}
