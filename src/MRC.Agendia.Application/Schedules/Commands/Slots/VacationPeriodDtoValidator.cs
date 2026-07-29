using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Schedules.Commands.Slots
{
    public class VacationPeriodDtoValidator : AbstractValidator<VacationPeriodDto>
    {
        public VacationPeriodDtoValidator()
        {
            // Absolute bounds so the day-by-day vacation loop cannot overflow
            // DateOnly.MaxValue (would be a 500 instead of a 400).
            RuleFor(x => x.From)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.To)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To debe ser igual o posterior a From.");
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}
