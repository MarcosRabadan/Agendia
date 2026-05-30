using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Slots
{
    public class VacationPeriodDtoValidator : AbstractValidator<VacationPeriodDto>
    {
        public VacationPeriodDtoValidator()
        {
            RuleFor(x => x.From).NotEqual(default(DateOnly));
            RuleFor(x => x.To)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To debe ser igual o posterior a From.");
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}
