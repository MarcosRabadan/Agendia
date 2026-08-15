namespace MRC.Agendia.Infrastructure.Notifications
{
    /// <summary>
    /// Options for the appointment reminder job, bound from the "Notifications" config
    /// section. Safe defaults, so the section is optional.
    /// </summary>
    public class ReminderOptions
    {
        public const string SectionName = "Notifications";

        /// <summary>How often the reminder job runs. Default 60 minutes.</summary>
        public int ReminderIntervalMinutes { get; set; } = 60;

        /// <summary>How far ahead a starting appointment triggers a reminder. Default 24 hours.</summary>
        public int ReminderWindowHours { get; set; } = 24;
    }
}
