using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Schedules.Commands.Slots;

namespace MRC.Agendia.Application.Schedules.Commands.Generation
{
    public class GenerateScheduleTemplateInputDtoValidator : AbstractValidator<GenerateScheduleTemplateInputDto>
    {
        public GenerateScheduleTemplateInputDtoValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.EffectiveFrom).NotEqual(default(DateOnly));
            RuleFor(x => x.EffectiveTo)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.EffectiveFrom)
                .WithMessage("EffectiveTo debe ser igual o posterior a EffectiveFrom.");
            RuleFor(x => x.WeeklySlots)
                .NotEmpty().WithMessage("Debe indicar al menos un slot semanal.");
            RuleForEach(x => x.WeeklySlots).SetValidator(new CreateWeeklyTimeSlotDtoValidator());
            RuleFor(x => x.WeeklySlots)
                .Must(slots => !WeeklySlotRules.HasIntraDayOverlap(slots))
                .WithMessage("Hay franjas que se solapan en el mismo dia de la semana.");
        }
    }
}
