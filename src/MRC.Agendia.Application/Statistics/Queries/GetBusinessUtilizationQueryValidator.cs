using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetBusinessUtilizationQueryValidator : AbstractValidator<GetBusinessUtilizationQuery>
    {
        /// <summary>
        /// Shorter than the stats range (366): utilization resolves the effective schedule
        /// day by day and walks every open minute, so a quarter is the sensible ceiling.
        /// </summary>
        public const int MaxRangeDays = 92;

        public GetBusinessUtilizationQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();

            // Absolute bounds keep To.AddDays(1) in the handler from overflowing at
            // DateOnly.MaxValue (a 400 instead of a 500).
            RuleFor(x => x.From)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.To)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To must be the same as or after From.");
            RuleFor(x => x)
                .Must(q => (q.To.DayNumber - q.From.DayNumber) + 1 <= MaxRangeDays)
                .WithMessage($"The utilization range cannot exceed {MaxRangeDays} days.");
        }
    }
}
