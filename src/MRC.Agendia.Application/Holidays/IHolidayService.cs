using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays
{
    public interface IHolidayService
    {
        /// <summary>Gets all holiday calendar entries.</summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A sequence of holiday calendar DTOs.</returns>
        Task<IEnumerable<HolidayCalendarDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets the holiday calendar entries for the given year.</summary>
        /// <param name="year">The calendar year to filter by.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A sequence of holiday calendar DTOs for the year.</returns>
        Task<IEnumerable<HolidayCalendarDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>Gets a holiday calendar entry by its identifier.</summary>
        /// <param name="id">The holiday identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The holiday calendar DTO, or <c>null</c> if not found.</returns>
        Task<HolidayCalendarDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Creates a new holiday calendar entry.</summary>
        /// <param name="dto">The data used to create the holiday.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created holiday calendar DTO.</returns>
        Task<HolidayCalendarDto> CreateAsync(CreateHolidayCalendarDto dto, CancellationToken cancellationToken = default);

        /// <summary>Updates an existing holiday calendar entry.</summary>
        /// <param name="dto">The data used to update the holiday, including its identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The updated holiday calendar DTO.</returns>
        Task<HolidayCalendarDto> UpdateAsync(UpdateHolidayCalendarDto dto, CancellationToken cancellationToken = default);

        /// <summary>Deletes a holiday calendar entry by its identifier.</summary>
        /// <param name="id">The holiday identifier.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns><c>true</c> when the holiday is deleted.</returns>
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
