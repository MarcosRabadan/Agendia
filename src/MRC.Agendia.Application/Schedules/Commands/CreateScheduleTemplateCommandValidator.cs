using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class CreateScheduleTemplateCommandValidator : AbstractValidator<CreateScheduleTemplateCommand>
    {
        public CreateScheduleTemplateCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.EffectiveFrom)
                .NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.EffectiveTo)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.Dto.EffectiveFrom)
                .WithMessage("EffectiveTo debe ser igual o posterior a EffectiveFrom.");
            RuleFor(x => x.Dto.WeeklySlots)
                .NotEmpty().WithMessage("Debe indicar al menos un slot semanal.");
            RuleForEach(x => x.Dto.WeeklySlots).SetValidator(new CreateWeeklyTimeSlotDtoValidator());
        }
    }
}
