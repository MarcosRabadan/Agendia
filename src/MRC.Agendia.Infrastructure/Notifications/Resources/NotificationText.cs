using System.Globalization;
using System.Resources;

namespace MRC.Agendia.Infrastructure.Notifications.Resources
{
    /// <summary>
    /// Accessor over the NotificationStrings .resx resource set. The culture is
    /// passed explicitly because notifications are composed outside of any HTTP
    /// request (the background reminder job, async best-effort sends), so there is
    /// no CurrentUICulture to rely on. Falls back to the resource key if a string
    /// is missing, which surfaces gaps without throwing.
    /// </summary>
    internal static class NotificationText
    {
        private static readonly ResourceManager Manager = new(
            "MRC.Agendia.Infrastructure.Notifications.Resources.NotificationStrings",
            typeof(NotificationText).Assembly);

        public static string Get(string key, CultureInfo culture)
            => Manager.GetString(key, culture) ?? key;

        public static string Format(string key, CultureInfo culture, params object[] args)
            => string.Format(Get(key, culture), args);
    }
}
