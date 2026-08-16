namespace MRC.Agendia.Domain.Exceptions
{
    public class EmployeeTimeOffNotFoundException : NotFoundException
    {
        public override string Code => "EMPLOYEE_TIME_OFF_NOT_FOUND";

        public EmployeeTimeOffNotFoundException(Guid id) : base($"Time off with Id {id} not found.")
        {
        }
    }
}
