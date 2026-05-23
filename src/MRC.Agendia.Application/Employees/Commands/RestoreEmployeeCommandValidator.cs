using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class RestoreEmployeeCommandValidator : AbstractValidator<RestoreEmployeeCommand>
    {
        public RestoreEmployeeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
