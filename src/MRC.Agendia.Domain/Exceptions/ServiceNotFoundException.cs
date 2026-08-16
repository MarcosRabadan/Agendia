namespace MRC.Agendia.Domain.Exceptions
{
    public class ServiceNotFoundException : NotFoundException
    {
        public override string Code => "SERVICE_NOT_FOUND";

        public ServiceNotFoundException(Guid id) : base($"Service with Id {id} not found.")
        {
        }
    }
}
