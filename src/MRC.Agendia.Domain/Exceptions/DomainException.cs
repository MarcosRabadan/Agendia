namespace MRC.Agendia.Domain.Exceptions
{
    /// <summary>
    /// Base for domain errors that carry a stable, machine-readable
    /// <see cref="Code"/> the API surfaces to clients. Intentionally does NOT
    /// derive from a BCL exception so a generic catch does not swallow it with
    /// the wrong HTTP semantics.
    /// </summary>
    public abstract class DomainException : Exception
    {
        public abstract string Code { get; }

        protected DomainException(string message) : base(message)
        {
        }
    }
}
