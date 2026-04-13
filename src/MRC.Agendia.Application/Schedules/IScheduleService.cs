using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules
{
    public interface IScheduleService
    {
        // Templates
        Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByBusinessIdAsync(int businessId);
        Task<ScheduleTemplateDto?> GetTemplateByIdAsync(int id);
        Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto);
        Task<ScheduleTemplateDto> UpdateTemplateAsync(UpdateScheduleTemplateDto dto);
        Task<bool> DeleteTemplateAsync(int id);

        // Overrides
        Task<IEnumerable<ScheduleOverrideDto>> GetOverridesByBusinessIdAsync(int businessId, DateOnly? from, DateOnly? to);
        Task<ScheduleOverrideDto?> GetOverrideByIdAsync(int id);
        Task<ScheduleOverrideDto> CreateOverrideAsync(CreateScheduleOverrideDto dto);
        Task<ScheduleOverrideDto> UpdateOverrideAsync(UpdateScheduleOverrideDto dto);
        Task<bool> DeleteOverrideAsync(int id);

        // Generate
        Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto);

        // Effective Schedule
        Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date);
        Task<IEnumerable<CalendarDayDto>> GetCalendarAsync(int businessId, DateOnly from, DateOnly to);
    }
}
