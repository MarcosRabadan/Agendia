using MRC.Agendia.Domain.Availability;

namespace MRC.Agendia.Application.Availability
{
    /// <summary>
    /// Pure, in-memory accounting of waitlist priority holds (#268) over a candidate window.
    /// Kept apart from the data access so the arithmetic can be unit-tested, and shared by
    /// everyone who has to agree on it: the availability read, the slot-capacity probe the
    /// waitlist uses, and the scheduling validator. Those three used to implement it three
    /// different ways and contradict each other (#308).
    ///
    /// <para><b>A hold reserves exactly one seat for exactly one client.</b> It is not a
    /// block on the slot: where there are more free seats than holds, the rest stay
    /// bookable. The difference only shows up with capacity &gt; 1 (a group class) or with
    /// several employees, which is why the old "drop the employee entirely" reading looked
    /// right for so long.</para>
    /// </summary>
    public static class SlotHoldCalculator
    {
        /// <summary>
        /// Counts the seats held by clients OTHER than <paramref name="requestingClientUserId"/>
        /// over [<paramref name="windowStart"/>, <paramref name="windowEnd"/>). The requester's
        /// own hold is skipped: it exists precisely so they can book the slot they were just
        /// offered. Pass null for a background job, which holds nothing and must see them all.
        /// </summary>
        /// <param name="holds">Active holds of the business on that date.</param>
        /// <param name="requestingClientUserId">Client asking, whose own holds do not count.</param>
        /// <param name="windowStart">Start of the candidate window (wall clock).</param>
        /// <param name="windowEnd">End of the candidate window, exclusive (wall clock).</param>
        /// <returns>Seats held per employee and on the business as a whole.</returns>
        public static HeldSeats Count(IReadOnlyList<SlotHold> holds,
                                      string? requestingClientUserId,
                                      DateTime windowStart,
                                      DateTime windowEnd)
        {
            var byEmployee = new Dictionary<Guid, int>();
            var anyEmployee = 0;

            foreach (var hold in holds)
            {
                if (requestingClientUserId is not null && hold.ClientUserId == requestingClientUserId)
                    continue;

                // Half-open overlap, the same test the agenda uses everywhere else. Comparing
                // start times alone let a 10:00 hold be booked over by 09:45-10:15.
                if (hold.Start >= windowEnd || hold.End <= windowStart)
                    continue;

                if (hold.EmployeeId is Guid employeeId)
                    byEmployee[employeeId] = byEmployee.TryGetValue(employeeId, out var seats) ? seats + 1 : 1;
                else
                    anyEmployee++;
            }

            return byEmployee.Count == 0 && anyEmployee == 0
                ? HeldSeats.None
                : new HeldSeats(byEmployee, anyEmployee);
        }
    }
}
