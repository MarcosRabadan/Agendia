namespace MRC.Agendia.Domain.Entities
{
    /// <summary>
    /// An additional service booked within an appointment, beyond its primary
    /// <see cref="Appointment.ServiceId"/> (e.g. cut + beard in one visit). The
    /// appointment's total duration and price are the sum of the primary service
    /// plus all of these. Owned by the appointment; not soft-deletable on its own.
    /// </summary>
    public class AppointmentExtraService
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int ServiceId { get; set; }

        public Appointment Appointment { get; set; } = null!;
        public Service Service { get; set; } = null!;
    }
}
