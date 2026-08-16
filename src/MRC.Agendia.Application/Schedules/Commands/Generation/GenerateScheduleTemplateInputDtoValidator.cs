using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Schedules.Commands.Slots;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Schedules.Commands.Generation
{
    public class GenerateScheduleTemplateInputDtoValidator : AbstractValidator<GenerateScheduleTemplateInputDto>
    {
        public GenerateScheduleTemplateInputDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            // Absolute bounds so the day-by-day generation loop cannot overflow
            // DateOnly.MaxValue (would be a 500 instead of a 400).
            RuleFor(x => x.EffectiveFrom)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.EffectiveTo)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.EffectiveFrom)
                .WithMessage("EffectiveTo must be the same as or after EffectiveFrom.");
            RuleFor(x => x.WeeklySlots)
                .NotEmpty().WithMessage("At least one weekly slot is required.");
            RuleForEach(x => x.WeeklySlots).SetValidator(new CreateWeeklyTimeSlotDtoValidator());
            RuleFor(x => x.WeeklySlots)
                .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                .WithMessage("There are overlapping slots on the same day of the week.");
        }
    }
}
