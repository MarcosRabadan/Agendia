using FluentValidation;
using MRC.Agendia.Application.Schedules.Commands.Slots;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public class CreateScheduleTemplateCommandValidator : AbstractValidator<CreateScheduleTemplateCommand>
    {
        public CreateScheduleTemplateCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).NotEmpty();
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.EffectiveFrom)
                .NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.EffectiveTo)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.Dto.EffectiveFrom)
                .WithMessage("EffectiveTo must be the same as or after EffectiveFrom.");
            RuleFor(x => x.Dto.WeeklySlots)
                .NotEmpty().WithMessage("At least one weekly slot is required.");
            RuleForEach(x => x.Dto.WeeklySlots).SetValidator(new CreateWeeklyTimeSlotDtoValidator());
            RuleFor(x => x.Dto.WeeklySlots)
                .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                .WithMessage("There are overlapping slots on the same day of the week.");
        }
    }
}
