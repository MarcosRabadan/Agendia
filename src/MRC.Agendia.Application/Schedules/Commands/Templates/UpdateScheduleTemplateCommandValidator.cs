using FluentValidation;
using MRC.Agendia.Application.Schedules.Commands.Slots;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public class UpdateScheduleTemplateCommandValidator : AbstractValidator<UpdateScheduleTemplateCommand>
    {
        public UpdateScheduleTemplateCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.EffectiveFrom).NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.EffectiveTo)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.Dto.EffectiveFrom)
                .WithMessage("EffectiveTo debe ser igual o posterior a EffectiveFrom.");
            RuleFor(x => x.Dto.WeeklySlots)
                .NotEmpty().WithMessage("Debe indicar al menos un slot semanal.");
            RuleForEach(x => x.Dto.WeeklySlots).SetValidator(new CreateWeeklyTimeSlotDtoValidator());
            RuleFor(x => x.Dto.WeeklySlots)
                .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                .WithMessage("Hay franjas que se solapan en el mismo dia de la semana.");
        }
    }
}
