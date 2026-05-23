namespace MRC.Agendia.Domain.Exceptions
{
    public class AppointmentNotFoundException : NotFoundException
    {
        public override string Code => "APPOINTMENT_NOT_FOUND";

        public AppointmentNotFoundException(int id) : base($"Cita con Id {id} no encontrada.")
        {
        }
    }
}
