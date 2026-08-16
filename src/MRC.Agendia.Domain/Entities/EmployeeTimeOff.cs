namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// An ad-hoc block on ONE employee's agenda (sick leave, a doctor's appointment,
    /// training) that does not touch the business's yearly schedule: the rest of the
    /// staff keeps working normally.
    ///
    /// <para><see cref="Start"/> and <see cref="End"/> are WALL-CLOCK times, like the
    /// appointment dates, and map to <c>timestamp without time zone</c>. The range is
    /// half-open [Start, End): a block ending at 13:00 leaves 13:00 bookable.</para>
    ///
    /// <para>A block takes the employee out entirely for that range, even when
    /// <c>MaxConcurrentAppointments</c> is greater than 1: a person who is away is away,
    /// their capacity is not merely reduced.</para>
    /// </summary>
    public class EmployeeTimeOff
    {
        public Guid Id { get; set; }

        public Guid EmployeeId { get; set; }

        /// <summary>Wall-clock start of the block (inclusive).</summary>
        public DateTime Start { get; set; }

        /// <summary>Wall-clock end of the block (exclusive).</summary>
        public DateTime End { get; set; }

        /// <summary>Free-text note for the staff (sick leave, training...). Optional.</summary>
        public string? Reason { get; set; }
    }
}
