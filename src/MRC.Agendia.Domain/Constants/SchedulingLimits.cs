namespace MRC.Agendia.Domain.Constants
{
    /// <summary>
    /// Absolute date bounds shared by the date-range validators. They keep user
    /// input well inside DateOnly's representable range so the downstream date maths
    /// (AddDays for half-open windows, day-by-day loops, recurrence expansion) can
    /// never overflow at DateOnly.MaxValue and surface as a 500 instead of a clean
    /// 400. Per-use-case window sizes (e.g. the 366-day range cap) are enforced
    /// separately. The upper bound matches the year cap already used by schedule
    /// generation (2000..2100).
    /// </summary>
    public static class SchedulingLimits
    {
        /// <summary>Earliest date any range endpoint may reference.</summary>
        public static readonly DateOnly MinDate = new(2000, 1, 1);

        /// <summary>Latest date any range endpoint may reference.</summary>
        public static readonly DateOnly MaxDate = new(2100, 12, 31);

        /// <summary>Uniform message for an out-of-bounds date. Keep it in sync with the bounds above.</summary>
        public const string OutOfRangeMessage = "The date must be between 2000-01-01 and 2100-12-31.";
    }
}
