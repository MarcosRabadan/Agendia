using FluentValidation;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Application.Schedules.Commands.Slots;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public class UpdateScheduleOverrideCommandValidator : AbstractValidator<UpdateScheduleOverrideCommand>
    {
        public UpdateScheduleOverrideCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Date).NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.OverrideType).IsInEnum();
            RuleFor(x => x.Dto.Reason).MaximumLength(500);

            When(x => x.Dto.OverrideType == ScheduleOverrideType.CustomHours, () =>
            {
                RuleFor(x => x.Dto.CustomSlots)
                    .NotEmpty()
                    .WithMessage("CustomHours requiere al menos un CustomSlot.");
                RuleForEach(x => x.Dto.CustomSlots!).SetValidator(new CreateCustomTimeSlotDtoValidator());
                RuleFor(x => x.Dto.CustomSlots)
                    .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                    .WithMessage("Hay franjas que se solapan en el mismo dia.");
            });
        }
    }
}
