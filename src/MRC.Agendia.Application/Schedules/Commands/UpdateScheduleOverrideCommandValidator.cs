using FluentValidation;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Schedules.Commands
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
            });
        }
    }
}
