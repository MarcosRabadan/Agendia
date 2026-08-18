namespace MRC.Agendia.Domain.Availability
{
    /// <summary>
    /// An active waitlist priority hold (#268) as the agenda sees it: ONE seat reserved for
    /// ONE client over a concrete wall-clock window, never a block on the whole slot.
    /// <see cref="EmployeeId"/> null means the seat is held on the business as a whole
    /// ("any employee"), so it comes off the business total instead of off one employee.
    /// </summary>
    /// <param name="ClientUserId">Harmony user id (the JWT sub) of the holder.</param>
    /// <param name="EmployeeId">Employee the seat is held on, or null for "any employee".</param>
    /// <param name="Start">Start of the held window (wall clock).</param>
    /// <param name="End">End of the held window, exclusive (wall clock).</param>
    public record SlotHold(
        string ClientUserId,
        Guid? EmployeeId,
        DateTime Start,
        DateTime End);
}
