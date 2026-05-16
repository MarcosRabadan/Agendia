using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
    {
        public DeleteEmployeeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
