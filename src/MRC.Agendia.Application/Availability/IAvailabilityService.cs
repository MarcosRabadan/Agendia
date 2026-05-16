using MRC.Agendia.Application.Availability.DTO;

namespace MRC.Agendia.Application.Availability
{
    public interface IAvailabilityService
    {
        /// <summary>
        /// Returns the available booking windows on a given date for a service
        /// at a business, optionally filtered by a specific employee.
        /// </summary>
        /// <param name="businessId">Business that offers the service.</param>
        /// <param name="date">Day to query.</param>
        /// <param name="serviceId">Service the client wants to book.</param>
        /// <param name="employeeId">Optional: limit results to this employee.</param>
        /// <param name="stepMinutes">
        /// Granularity of candidate start times in minutes (default 15).
        /// A smaller step yields more bookable windows.
        /// </param>
        Task<AvailabilityDto> GetAvailabilityAsync(
            int businessId,
            DateOnly date,
            int serviceId,
            int? employeeId,
            int stepMinutes = 15);
    }
}
