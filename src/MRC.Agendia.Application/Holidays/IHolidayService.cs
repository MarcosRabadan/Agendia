using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays
{
    public interface IHolidayService
    {
        Task<IEnumerable<HolidayCalendarDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<HolidayCalendarDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<HolidayCalendarDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<HolidayCalendarDto> CreateAsync(CreateHolidayCalendarDto dto, CancellationToken cancellationToken = default);
        Task<HolidayCalendarDto> UpdateAsync(UpdateHolidayCalendarDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
