using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays
{
    public interface IHolidayService
    {
        Task<IEnumerable<HolidayCalendarDto>> GetAllAsync();
        Task<IEnumerable<HolidayCalendarDto>> GetByYearAsync(int year);
        Task<HolidayCalendarDto?> GetByIdAsync(int id);
        Task<HolidayCalendarDto> CreateAsync(CreateHolidayCalendarDto dto);
        Task<HolidayCalendarDto> UpdateAsync(UpdateHolidayCalendarDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
