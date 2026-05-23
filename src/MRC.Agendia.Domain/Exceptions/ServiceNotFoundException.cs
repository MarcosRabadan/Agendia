namespace MRC.Agendia.Domain.Exceptions
{
    public class ServiceNotFoundException : NotFoundException
    {
        public override string Code => "SERVICE_NOT_FOUND";

        public ServiceNotFoundException(int id) : base($"Servicio con Id {id} no encontrado.")
        {
        }
    }
}
