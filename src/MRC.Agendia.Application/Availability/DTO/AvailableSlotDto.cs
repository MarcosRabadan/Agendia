namespace MRC.Agendia.Application.Availability.DTO
{
    /// <summary>
    /// A single bookable window. <see cref="StartTime"/> is the proposed
    /// appointment start; <see cref="EndTime"/> equals start + service duration.
    /// <see cref="AvailableEmployeeIds"/> contains every employee that is free
    /// to take this booking (the caller can pick one or let the system choose).
    /// </summary>
    public record AvailableSlotDto(
        TimeOnly StartTime,
        TimeOnly EndTime,
        IReadOnlyList<int> AvailableEmployeeIds);
}
