using FluentValidation;
using MRC.Agendia.Application.Schedules.Commands;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class PreviewScheduleQueryValidator : AbstractValidator<PreviewScheduleQuery>
    {
        public PreviewScheduleQueryValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.Year)
                .GreaterThanOrEqualTo(2000)
                .LessThanOrEqualTo(2100);
            RuleFor(x => x.Dto.Templates)
                .NotEmpty().WithMessage("Debe indicar al menos una plantilla.");
            RuleForEach(x => x.Dto.Templates).SetValidator(new GenerateScheduleTemplateInputDtoValidator());

            When(x => x.Dto.VacationPeriods != null, () =>
            {
                RuleForEach(x => x.Dto.VacationPeriods!).SetValidator(new VacationPeriodDtoValidator());
            });

            When(x => x.Dto.CustomClosedDates != null, () =>
            {
                RuleForEach(x => x.Dto.CustomClosedDates!).SetValidator(new ClosedDateDtoValidator());
            });
        }
    }
}
