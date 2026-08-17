namespace MRC.Agendia.Domain.Interfaces
{
    /// <summary>
    /// The only part of an appointment integration event that does NOT live on the appointment
    /// itself: the business that owns it and the language it notifies in. Resolved from the
    /// appointment's employee, so the caller builds the rest of the event from the entity it
    /// already has in hand.
    /// </summary>
    public sealed record AppointmentNotificationBusiness(Guid BusinessId, string Language);
}
