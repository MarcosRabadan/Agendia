using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Application.Events
{
    /// <summary>
    /// Publishes an integration event. The implementation enlists the event into
    /// the current unit of work (a transactional outbox): the event is written by
    /// the same <c>SaveChanges</c> as the operation that raised it, so it is never
    /// lost if the broker is down. A separate dispatcher delivers it afterwards.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Enlists <paramref name="event"/> to be persisted with the current
        /// unit of work. It does NOT call SaveChanges: the caller's Save persists
        /// the event atomically with the domain change.
        /// </summary>
        /// <param name="event">The integration event to publish.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
    }
}
