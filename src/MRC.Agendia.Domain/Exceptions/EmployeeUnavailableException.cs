namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The employee has a time-off block covering the requested time, so the appointment
    /// cannot be created or moved there. Maps to HTTP 400.
    /// </summary>
    public class EmployeeUnavailableException : DomainException
    {
        public override string Code => "EMPLOYEE_UNAVAILABLE";

        public EmployeeUnavailableException()
            : base("The employee is not available at that time (time off).")
        {
        }
    }
}
