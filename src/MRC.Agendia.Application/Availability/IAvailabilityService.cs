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
        /// <param name="extraServiceIds">
        /// Optional additional services booked in the same visit (#170). When
        /// present, slots are sized to fit the total duration (primary + extras).
        /// </param>
        Task<AvailabilityDto> GetAvailabilityAsync(
            int businessId,
            DateOnly date,
            int serviceId,
            int? employeeId,
            int stepMinutes = 15,
            IReadOnlyCollection<int>? extraServiceIds = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Free capacity of one exact slot (start time + service duration) for a
        /// service, optionally limited to an employee. Returns null when the slot
        /// is not bookable at all (day closed, outside the open windows, no active
        /// employee); 0 means the slot exists but is full; &gt; 0 means it has room.
        /// </summary>
        Task<int?> GetSlotCapacityAsync(
            int businessId,
            DateOnly date,
            TimeOnly startTime,
            int serviceId,
            int? employeeId,
            CancellationToken cancellationToken = default);
    }
}
