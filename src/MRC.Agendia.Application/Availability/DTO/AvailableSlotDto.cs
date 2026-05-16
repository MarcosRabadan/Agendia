namespace MRC.Agendia.Application.Availability.DTO
{
    /// <summary>
    /// A single bookable window. <see cref="StartTime"/> is the proposed
    /// appointment start; <see cref="EndTime"/> equals start + service duration.
    ///
    /// <see cref="Capacity"/> is the number of simultaneous bookings the
    /// business can accept at this exact slot. It equals the sum of the
    /// remaining capacity of each available employee (an employee with
    /// MaxConcurrentAppointments=2 that already has 1 booking contributes 1).
    ///
    /// <see cref="AvailableEmployeeIds"/> lists the distinct employees that
    /// still have at least one free spot at this time.
    /// </summary>
    public record AvailableSlotDto(
        TimeOnly StartTime,
        TimeOnly EndTime,
        int Capacity,
        IReadOnlyList<int> AvailableEmployeeIds);
}
