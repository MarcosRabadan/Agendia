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
    }
}
