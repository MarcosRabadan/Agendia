using FluentValidation;

namespace MRC.Agendia.Application.Schedules.Commands.Templates
{
    public class DeleteScheduleTemplateCommandValidator : AbstractValidator<DeleteScheduleTemplateCommand>
    {
        public DeleteScheduleTemplateCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
