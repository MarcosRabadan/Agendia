namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// The broker adapter the outbox dispatcher delivers events through. This is
    /// the single seam to swap for a real transport (RabbitMQ, Azure Service Bus,
    /// Kafka...) without touching the publisher, the outbox or the dispatcher.
    /// The current implementation logs the event.
    /// </summary>
    public interface IEventTransport
    {
        /// <summary>Delivers one serialized event to the broker.</summary>
        /// <param name="type">Event type discriminator.</param>
        /// <param name="payload">JSON-serialized event payload.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task PublishAsync(string type, string payload, CancellationToken cancellationToken = default);
    }
}
