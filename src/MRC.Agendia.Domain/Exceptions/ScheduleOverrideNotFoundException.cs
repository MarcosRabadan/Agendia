namespace MRC.Agendia.Domain.Exceptions
{
    public class ScheduleOverrideNotFoundException : NotFoundException
    {
        public override string Code => "SCHEDULE_OVERRIDE_NOT_FOUND";

        public ScheduleOverrideNotFoundException(int id) : base($"Excepcion de horario con Id {id} no encontrada.")
        {
        }
    }
}
