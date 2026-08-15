namespace MRC.Agendia.Domain.Exceptions
{
    public class BusinessNotFoundException : NotFoundException
    {
        public override string Code => "BUSINESS_NOT_FOUND";

        public BusinessNotFoundException(Guid id) : base($"Negocio con Id {id} no encontrado.")
        {
        }
    }
}
