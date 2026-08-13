namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Marker for an event Agendia publishes for other services to consume
    /// (an integration event). Agendia no longer delivers notifications itself:
    /// it records what happened and a dedicated notifications/identity service
    /// resolves the recipient's contact details and delivers.
    /// </summary>
    public interface IIntegrationEvent
    {
        /// <summary>UTC instant at which the event occurred.</summary>
        DateTime OccurredOnUtc { get; }
    }
}
