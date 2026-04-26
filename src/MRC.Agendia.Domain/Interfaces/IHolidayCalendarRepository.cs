using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IHolidayCalendarRepository
    {
        Task<HolidayCalendar?> GetByIdAsync(int id);
        Task<IEnumerable<HolidayCalendar>> GetAllAsync();
        Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year);
        Task<IEnumerable<HolidayCalendar>> GetByYearAndRegionAsync(int year, string? region);
        Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, string? region = null);
        Task AddAsync(HolidayCalendar holiday);
        Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays);
        void Update(HolidayCalendar holiday);
        void Delete(HolidayCalendar holiday);
    }
}
