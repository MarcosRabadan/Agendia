namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The selected employee is inactive and cannot take appointments. Maps to HTTP 400.
    /// </summary>
    public class EmployeeInactiveException : DomainException
    {
        public override string Code => "EMPLOYEE_INACTIVE";

        public EmployeeInactiveException() : base("El empleado indicado esta inactivo.")
        {
        }
    }
}
