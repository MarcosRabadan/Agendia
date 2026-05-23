using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules
{
    public interface IScheduleService
    {
        // Templates
        Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByBusinessIdAsync(int businessId, CancellationToken cancellationToken = default);
        Task<ScheduleTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto, CancellationToken cancellationToken = default);
        Task<ScheduleTemplateDto> UpdateTemplateAsync(UpdateScheduleTemplateDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteTemplateAsync(int id, CancellationToken cancellationToken = default);

        // Overrides
        Task<IEnumerable<ScheduleOverrideDto>> GetOverridesByBusinessIdAsync(int businessId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
        Task<ScheduleOverrideDto?> GetOverrideByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ScheduleOverrideDto> CreateOverrideAsync(CreateScheduleOverrideDto dto, CancellationToken cancellationToken = default);
        Task<ScheduleOverrideDto> UpdateOverrideAsync(UpdateScheduleOverrideDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteOverrideAsync(int id, CancellationToken cancellationToken = default);

        // Effective Schedule
        Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default);
        Task<IEnumerable<CalendarDayDto>> GetCalendarAsync(int businessId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
    }
}
