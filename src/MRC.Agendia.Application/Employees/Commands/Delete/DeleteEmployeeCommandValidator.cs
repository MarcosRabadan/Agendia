using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Delete
{
    public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
    {
        public DeleteEmployeeCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
