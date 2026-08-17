using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Common
{
    /// <summary>
    /// Validation rule for the absolute date bounds the scheduling code supports
    /// (<see cref="SchedulingLimits"/>).
    /// </summary>
    /// <remarks>
    /// A wild date is a 400 here rather than an overflow deeper down: several flows shift dates
    /// around (<c>AddDays</c> on a series move, <c>AddDays(1)</c> to close a day range), and near
    /// <see cref="DateOnly.MaxValue"/> that throws and surfaces as a 500. The series and schedule
    /// validators have bounded their inputs since the start; the appointment ones joined in #295
    /// so the whole surface behaves the same way.
    /// </remarks>
    public static class SupportedDateRangeRules
    {
        /// <summary>Requires the date part of the value to fall inside <see cref="SchedulingLimits"/>.</summary>
        public static IRuleBuilderOptions<T, DateTime> MustBeWithinSupportedDates<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
            => ruleBuilder
                .Must(value =>
                {
                    var date = DateOnly.FromDateTime(value);
                    return date >= SchedulingLimits.MinDate && date <= SchedulingLimits.MaxDate;
                })
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
    }
}
