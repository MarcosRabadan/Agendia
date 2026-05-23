namespace MRC.Agendia.Domain.Exceptions
{
    public class EmployeeNotFoundException : NotFoundException
    {
        public override string Code => "EMPLOYEE_NOT_FOUND";

        public EmployeeNotFoundException(int id) : base($"Empleado con Id {id} no encontrado.")
        {
        }
    }
}
