namespace MRC.Agendia.Domain.Enums
{
    public static class AppointmentStatusExtensions
    {
        /// <summary>
        /// Whether an appointment in this status takes up a slot for capacity and
        /// conflict checks. Availability and the scheduling validator MUST use the
        /// same rule so what the client sees as free matches what booking accepts.
        /// </summary>
        public static bool OccupiesCapacity(this AppointmentStatus status)
            => status is AppointmentStatus.Pending or AppointmentStatus.Confirmed;

        /// <summary>
        /// Terminal statuses: once an appointment reaches one of these it is finished
        /// and its status must not change again (no Completed/NoShow/Cancelled to
        /// anything else). Editing notes or other fields without touching the status
        /// is still allowed.
        /// </summary>
        public static bool IsTerminal(this AppointmentStatus status)
            => status is AppointmentStatus.Completed or AppointmentStatus.NoShow or AppointmentStatus.Cancelled;
    }
}
