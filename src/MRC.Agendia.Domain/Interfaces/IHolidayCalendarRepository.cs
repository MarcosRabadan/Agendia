using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IHolidayCalendarRepository
    {
        /// <summary>Gets a tracked holiday by id.</summary>
        /// <param name="id">Holiday id.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The holiday, or null when missing.</returns>
        Task<HolidayCalendar?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Gets all holidays ordered by date. Untracked.</summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>All holidays.</returns>
        Task<IEnumerable<HolidayCalendar>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets the holidays of a year ordered by date. Untracked.</summary>
        /// <param name="year">Calendar year.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The holidays of that year.</returns>
        Task<IEnumerable<HolidayCalendar>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>Gets the holidays whose date is within [from, to] (inclusive), ordered by date. Untracked.</summary>
        /// <param name="from">Range start (inclusive).</param>
        /// <param name="to">Range end (inclusive).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The holidays in the range.</returns>
        Task<IEnumerable<HolidayCalendar>> GetByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

        /// <summary>Adds a new holiday to the context.</summary>
        /// <param name="holiday">The holiday to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddAsync(HolidayCalendar holiday, CancellationToken cancellationToken = default);

        /// <summary>Adds several holidays to the context in one call.</summary>
        /// <param name="holidays">The holidays to add.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task AddRangeAsync(IEnumerable<HolidayCalendar> holidays, CancellationToken cancellationToken = default);

        /// <summary>Marks a holiday as modified.</summary>
        /// <param name="holiday">The holiday to update.</param>
        void Update(HolidayCalendar holiday);

        /// <summary>Removes a holiday from the context (hard delete; holidays are not soft-deletable).</summary>
        /// <param name="holiday">The holiday to delete.</param>
        void Delete(HolidayCalendar holiday);
    }
}
