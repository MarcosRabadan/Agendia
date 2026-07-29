using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetBusinessStatsQueryValidator : AbstractValidator<GetBusinessStatsQuery>
    {
        private const int MaxRangeDays = 366;

        public GetBusinessStatsQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            // Absolute bounds keep To.AddDays(1) in the handler from overflowing at
            // DateOnly.MaxValue (would be a 500 instead of a 400); NotEqual(default)
            // is subsumed because default(DateOnly) falls below MinDate.
            RuleFor(x => x.From)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.To)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To debe ser igual o posterior a From.");
            RuleFor(x => x)
                .Must(q => (q.To.DayNumber - q.From.DayNumber) + 1 <= MaxRangeDays)
                .WithMessage($"El rango de estadisticas no puede superar {MaxRangeDays} dias.");
        }
    }
}
