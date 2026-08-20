using FluentValidation;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    public class DeleteEmployeeTimeOffCommandValidator : AbstractValidator<DeleteEmployeeTimeOffCommand>
    {
        public DeleteEmployeeTimeOffCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.TimeOffId).NotEmpty();
        }
    }
}
