namespace MRC.Agendia.Domain.Events
{
    /// <summary>
    /// Raised when a slot a client was waiting for frees up (FIFO). The consumer
    /// tells the client (resolved from <see cref="ClientUserId"/>) in
    /// <see cref="Language"/> that they can now book. The slot is held for them until
    /// <see cref="HoldUntil"/> (UTC): nobody else can take it before then, so the message
    /// should say how long they have. <see cref="EmployeeId"/> is null for an "any
    /// employee" waitlist entry.
    /// </summary>
    public sealed record WaitlistSlotAvailable(Guid WaitlistEntryId,
                                               Guid BusinessId,
                                               Guid? EmployeeId,
                                               string ClientUserId,
                                               Guid ServiceId,
                                               DateOnly Date,
                                               TimeOnly StartTime,
                                               DateTime HoldUntil,
                                               string Language,
                                               DateTime OccurredOnUtc) : IIntegrationEvent;
}
