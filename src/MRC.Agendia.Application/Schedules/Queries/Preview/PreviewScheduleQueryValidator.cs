using FluentValidation;
using MRC.Agendia.Application.Schedules.Commands;
using MRC.Agendia.Application.Schedules.Commands.Generation;

namespace MRC.Agendia.Application.Schedules.Queries.Preview
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
