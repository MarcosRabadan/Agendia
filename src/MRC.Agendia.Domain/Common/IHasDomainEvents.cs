using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Domain.Common
{
    /// <summary>
    /// An entity that records integration events while its state changes; they are enlisted
    /// into the transactional outbox when the entity is saved (see the DbContext's SaveChanges
    /// override), so the event and the state change commit together.
    /// </summary>
    public interface IHasDomainEvents
    {
        /// <summary>Events raised by this entity and not yet enlisted into the outbox.</summary>
        IReadOnlyCollection<IIntegrationEvent> DomainEvents { get; }

        /// <summary>Clears the pending events (called after they have been enlisted).</summary>
        void ClearDomainEvents();
    }
}
