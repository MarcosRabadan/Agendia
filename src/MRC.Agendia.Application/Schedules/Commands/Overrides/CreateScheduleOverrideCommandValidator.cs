using FluentValidation;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Application.Schedules.Commands.Slots;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public class CreateScheduleOverrideCommandValidator : AbstractValidator<CreateScheduleOverrideCommand>
    {
        public CreateScheduleOverrideCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).NotEmpty();
            RuleFor(x => x.Dto.Date).NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.OverrideType).IsInEnum();
            RuleFor(x => x.Dto.Reason).MaximumLength(500);

            // CustomHours overrides require at least one CustomSlot.
            When(x => x.Dto.OverrideType == ScheduleOverrideType.CustomHours, () =>
            {
                RuleFor(x => x.Dto.CustomSlots)
                    .NotEmpty()
                    .WithMessage("CustomHours requires at least one CustomSlot.");
                RuleForEach(x => x.Dto.CustomSlots!).SetValidator(new CreateCustomTimeSlotDtoValidator());
                RuleFor(x => x.Dto.CustomSlots)
                    .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                    .WithMessage("There are overlapping slots on the same day.");
            });
        }
    }
}
