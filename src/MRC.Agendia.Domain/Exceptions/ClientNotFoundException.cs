namespace MRC.Agendia.Domain.Exceptions
{
    public class ClientNotFoundException : NotFoundException
    {
        public override string Code => "CLIENT_NOT_FOUND";

        public ClientNotFoundException(int id) : base($"Cliente con Id {id} no encontrado.")
        {
        }
    }
}
