using FluentValidation;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class GenerateScheduleCommandValidator : AbstractValidator<GenerateScheduleCommand>
    {
        public GenerateScheduleCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto).SetValidator(new GenerateScheduleRequestDtoValidator());
        }
    }
}
