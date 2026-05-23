namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>An account already exists for the email. Maps to HTTP 400.</summary>
    public class DuplicateEmailException : DomainException
    {
        public override string Code => "DUPLICATE_EMAIL";

        public DuplicateEmailException() : base("Ya existe un usuario con ese email.")
        {
        }
    }
}
