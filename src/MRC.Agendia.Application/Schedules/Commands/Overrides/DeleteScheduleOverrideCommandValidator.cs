using FluentValidation;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public class DeleteScheduleOverrideCommandValidator : AbstractValidator<DeleteScheduleOverrideCommand>
    {
        public DeleteScheduleOverrideCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
