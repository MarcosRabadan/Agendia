namespace MRC.Agendia.Domain.Enums
{
    // Drives how a recurring appointment series is expanded into concrete dates.
    // Values are pinned so the JSON contract stays stable. "Biweekly" is modelled
    // as Weekly with an interval of 2 rather than a separate member.
    public enum RecurrenceFrequency
    {
        Weekly = 0,
        Monthly = 1
    }
}
