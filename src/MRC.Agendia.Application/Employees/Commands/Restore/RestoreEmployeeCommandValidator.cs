using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Restore
{
    public class RestoreEmployeeCommandValidator : AbstractValidator<RestoreEmployeeCommand>
    {
        public RestoreEmployeeCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
