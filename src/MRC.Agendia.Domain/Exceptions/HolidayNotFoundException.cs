namespace MRC.Agendia.Domain.Exceptions
{
    public class HolidayNotFoundException : NotFoundException
    {
        public override string Code => "HOLIDAY_NOT_FOUND";

        public HolidayNotFoundException(Guid id) : base($"Festivo con Id {id} no encontrado.")
        {
        }
    }
}
