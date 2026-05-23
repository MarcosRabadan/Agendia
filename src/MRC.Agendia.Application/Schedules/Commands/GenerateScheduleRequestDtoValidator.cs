using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    /// <summary>
    /// Shared validation rules for a schedule generation request, reused by both
    /// the generate command and the preview query (which carry the same DTO).
    /// </summary>
    public class GenerateScheduleRequestDtoValidator : AbstractValidator<GenerateScheduleRequestDto>
    {
        public GenerateScheduleRequestDtoValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            RuleFor(x => x.Year)
                .GreaterThanOrEqualTo(2000)
                .LessThanOrEqualTo(2100);
            RuleFor(x => x.Templates)
                .NotEmpty().WithMessage("Debe indicar al menos una plantilla.");
            RuleForEach(x => x.Templates).SetValidator(new GenerateScheduleTemplateInputDtoValidator());

            When(x => x.VacationPeriods != null, () =>
            {
                RuleForEach(x => x.VacationPeriods!).SetValidator(new VacationPeriodDtoValidator());
            });

            When(x => x.CustomClosedDates != null, () =>
            {
                RuleForEach(x => x.CustomClosedDates!).SetValidator(new ClosedDateDtoValidator());
            });
        }
    }
}
