using System.ComponentModel.DataAnnotations.Schema;
using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Domain.Common
{
    /// <summary>
    /// Base for domain entities: records integration events (<see cref="IHasDomainEvents"/>)
    /// raised while the entity's state changes. The DbContext's SaveChanges override enlists
    /// them into the transactional outbox in the same save, so the event and the state change
    /// commit together.
    /// </summary>
    public abstract class Entity : IHasDomainEvents
    {
        private readonly List<IIntegrationEvent> _domainEvents = new();

        /// <inheritdoc />
        [NotMapped]
        public IReadOnlyCollection<IIntegrationEvent> DomainEvents => _domainEvents;

        /// <summary>Records an integration event to be enlisted into the outbox on save.</summary>
        public void RaiseEvent(IIntegrationEvent @event) => _domainEvents.Add(@event);

        /// <inheritdoc />
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
