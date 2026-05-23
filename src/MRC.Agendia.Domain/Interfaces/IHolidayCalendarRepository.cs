using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IHolidayCalendarRepository
    {
        Task<HolidayCalendar?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<HolidayCalendar>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
        Task AddAsync(HolidayCalendar holiday, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays, CancellationToken cancellationToken = default);
        void Update(HolidayCalendar holiday);
        void Delete(HolidayCalendar holiday);
    }
}
