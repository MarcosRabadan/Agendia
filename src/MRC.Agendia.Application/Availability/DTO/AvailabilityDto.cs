namespace MRC.Agendia.Application.Availability.DTO
{
    /// <summary>
    /// Result of asking "what time slots can I book on this day?".
    /// If <see cref="IsOpen"/> is false, <see cref="Slots"/> is empty and
    /// <see cref="ClosedReason"/> explains why (holiday, vacation, no schedule…).
    /// </summary>
    public record AvailabilityDto(
        DateOnly Date,
        int BusinessId,
        int ServiceId,
        int? EmployeeId,
        int DurationMinutes,
        int StepMinutes,
        bool IsOpen,
        string? ClosedReason,
        IReadOnlyList<AvailableSlotDto> Slots);
}
