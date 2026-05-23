namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// The service and the employee belong to different businesses. Maps to HTTP 400.
    /// </summary>
    public class ServiceEmployeeMismatchException : DomainException
    {
        public override string Code => "SERVICE_EMPLOYEE_MISMATCH";

        public ServiceEmployeeMismatchException() : base("El servicio y el empleado pertenecen a negocios distintos.")
        {
        }
    }
}
