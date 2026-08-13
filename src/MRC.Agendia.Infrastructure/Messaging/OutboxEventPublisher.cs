using System.Text.Json;
using MRC.Agendia.Application.Events;
using MRC.Agendia.Domain.Events;

namespace MRC.Agendia.Infrastructure.Messaging
{
    /// <summary>
    /// Transactional-outbox implementation of <see cref="IEventPublisher"/>:
    /// serializes the event and enlists it into the current DbContext as an
    /// <see cref="OutboxMessage"/> WITHOUT saving. The caller's unit-of-work Save
    /// then persists the event atomically with the domain change.
    /// </summary>
    public class OutboxEventPublisher : IEventPublisher
    {
        internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly AgendiaDbContext _context;

        public OutboxEventPublisher(AgendiaDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                // Serialize against the runtime type so the concrete event's
                // properties are written, not just the marker interface.
                Type = @event.GetType().Name,
                Payload = JsonSerializer.Serialize(@event, @event.GetType(), SerializerOptions),
                OccurredOnUtc = @event.OccurredOnUtc,
            };

            _context.Set<OutboxMessage>().Add(message);
            return Task.CompletedTask;
        }
    }
}
