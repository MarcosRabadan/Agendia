namespace MRC.Agendia.Domain.Exceptions
{
    public class AppointmentNotFoundException : NotFoundException
    {
        public override string Code => "APPOINTMENT_NOT_FOUND";

        public AppointmentNotFoundException(Guid id) : base($"Appointment with Id {id} not found.")
        {
        }
    }
}
