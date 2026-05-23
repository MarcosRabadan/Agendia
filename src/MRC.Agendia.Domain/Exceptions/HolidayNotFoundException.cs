namespace MRC.Agendia.Domain.Exceptions
{
    public class HolidayNotFoundException : NotFoundException
    {
        public override string Code => "HOLIDAY_NOT_FOUND";

        public HolidayNotFoundException(int id) : base($"Holiday with Id {id} not found.")
        {
        }
    }
}
