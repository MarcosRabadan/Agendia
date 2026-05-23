namespace MRC.Agendia.Domain.Exceptions
{
    public class EmployeeNotFoundException : NotFoundException
    {
        public override string Code => "EMPLOYEE_NOT_FOUND";

        public EmployeeNotFoundException(int id) : base($"Employee with Id {id} not found.")
        {
        }
    }
}
