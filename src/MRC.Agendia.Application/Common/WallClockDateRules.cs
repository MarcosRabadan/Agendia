using FluentValidation;

namespace MRC.Agendia.Application.Common
{
    /// <summary>
    /// Validation rule for the agenda's wall-clock dates (#290).
    /// </summary>
    /// <remarks>
    /// An appointment's start/end and an employee time-off range are wall-clock times in the
    /// business time zone, persisted in <c>timestamp without time zone</c> columns. A value
    /// that carries a zone is therefore not a formatting detail but a different instant:
    /// "09:00Z" is 11:00 in Madrid in summer. Left unchecked it reaches persistence, where
    /// Npgsql either refuses the value outright (<see cref="DateTimeKind.Utc"/>) or stores it
    /// shifted to whatever offset the server runs in (<see cref="DateTimeKind.Local"/>) - a
    /// booking silently made at the wrong hour. Rejecting it up front keeps the ambiguity out
    /// of the domain: callers send the wall-clock time and nothing else.
    ///
    /// This does NOT apply to inputs that are genuine UTC instants (for instance the
    /// audit-log time filter), where a zone is the correct thing to send.
    /// </remarks>
    public static class WallClockDateRules
    {
        /// <summary>
        /// Requires the value to be a bare wall-clock time, with no "Z" and no offset.
        /// </summary>
        public static IRuleBuilderOptions<T, DateTime> MustBeWallClock<T>(this IRuleBuilder<T, DateTime> ruleBuilder)
            => ruleBuilder
                .Must(value => value.Kind == DateTimeKind.Unspecified)
                .WithMessage("{PropertyName} must be a wall-clock time in the business time zone, "
                             + "without a time zone: send '2026-09-01T09:00:00', not 'Z' nor an offset.");
    }
}
