using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.TimeOff.Queries
{
    public class GetEmployeeTimeOffQueryValidator : AbstractValidator<GetEmployeeTimeOffQuery>
    {
        public GetEmployeeTimeOffQueryValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();

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
        }
    }
}
