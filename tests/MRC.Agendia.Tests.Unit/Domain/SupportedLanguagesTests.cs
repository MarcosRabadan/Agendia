using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Tests.Unit.Domain
{
    /// <summary>
    /// Coverage for the supported-language whitelist used by the business validators
    /// and the notification localization: recognition is case-insensitive and trimmed,
    /// and normalization always falls back to Spanish for anything unknown.
    /// </summary>
    public class SupportedLanguagesTests
    {
        [Theory]
        [InlineData("es")]
        [InlineData("en")]
        [InlineData("fr")]
        [InlineData("ES")]
        [InlineData("  Fr  ")]
        public void IsSupported_true_for_known_codes(string code)
            => Assert.True(SupportedLanguages.IsSupported(code));

        [Theory]
        [InlineData("de")]
        [InlineData("pt")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void IsSupported_false_for_unknown_or_blank(string? code)
            => Assert.False(SupportedLanguages.IsSupported(code));

        [Theory]
        [InlineData("es", "es")]
        [InlineData("EN", "en")]
        [InlineData("  fr ", "fr")]
        public void Normalize_returns_lowercased_trimmed_code(string input, string expected)
            => Assert.Equal(expected, SupportedLanguages.Normalize(input));

        [Theory]
        [InlineData("de")]
        [InlineData("")]
        [InlineData(null)]
        public void Normalize_falls_back_to_spanish(string? input)
            => Assert.Equal(SupportedLanguages.Spanish, SupportedLanguages.Normalize(input));
    }
}
