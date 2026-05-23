using FluentValidation;
using MRC.Agendia.Application.Schedules.Commands;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class PreviewScheduleQueryValidator : AbstractValidator<PreviewScheduleQuery>
    {
        public PreviewScheduleQueryValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto).SetValidator(new GenerateScheduleRequestDtoValidator());
        }
    }
}
