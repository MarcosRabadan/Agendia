namespace MRC.Agendia.Application.TimeOff.DTO
{
    /// <summary>
    /// The block just created, plus the appointments that were ALREADY booked inside it.
    /// Those are left untouched on purpose - the block only stops new bookings - and are
    /// reported so the staff can move or cancel them.
    /// </summary>
    public record CreateEmployeeTimeOffResultDto(
        EmployeeTimeOffDto TimeOff,
        IReadOnlyList<Guid> CollidingAppointmentIds);
}
