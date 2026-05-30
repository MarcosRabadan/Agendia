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
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The day's availability: the open windows and the bookable slots with their capacity.</returns>
        Task<AvailabilityDto> GetAvailabilityAsync(int businessId,
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
        /// <param name="businessId">Business that offers the service.</param>
        /// <param name="date">Day of the slot.</param>
        /// <param name="startTime">Start time of the slot.</param>
        /// <param name="serviceId">Service whose duration defines the slot length.</param>
        /// <param name="employeeId">Optional: limit the capacity to this employee.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Free capacity of the slot; null when it is not bookable, 0 when full, &gt; 0 when it has room.</returns>
        Task<int?> GetSlotCapacityAsync(int businessId,
                                        DateOnly date,
                                        TimeOnly startTime,
                                        int serviceId,
                                        int? employeeId,
                                        CancellationToken cancellationToken = default);
    }
}
