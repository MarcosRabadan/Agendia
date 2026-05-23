namespace MRC.Agendia.Domain.Exceptions
{
    public class ScheduleTemplateNotFoundException : NotFoundException
    {
        public override string Code => "SCHEDULE_TEMPLATE_NOT_FOUND";

        public ScheduleTemplateNotFoundException(int id) : base($"Plantilla de horario con Id {id} no encontrada.")
        {
        }
    }
}
