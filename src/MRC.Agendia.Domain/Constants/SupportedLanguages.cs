namespace MRC.Agendia.Domain.Constants
{
    /// <summary>
    /// Languages the product can localize notifications into. The value stored in
    /// <see cref="Entities.Business.DefaultLanguage"/> is one of these two-letter
    /// codes; Spanish is both the default and the fallback for any unknown value.
    /// </summary>
    public static class SupportedLanguages
    {
        public const string Spanish = "es";
        public const string English = "en";
        public const string French = "fr";

        /// <summary>All supported language codes.</summary>
        public static readonly string[] All = { Spanish, English, French };

        /// <summary>True if the code is supported (case-insensitive, trimmed).</summary>
        public static bool IsSupported(string? language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return false;

            var normalized = language.Trim().ToLowerInvariant();
            return Array.Exists(All, code => code == normalized);
        }

        /// <summary>
        /// Returns the normalized supported code, or <see cref="Spanish"/> when the
        /// value is null, blank or unsupported.
        /// </summary>
        public static string Normalize(string? language)
            => IsSupported(language) ? language!.Trim().ToLowerInvariant() : Spanish;
    }
}
