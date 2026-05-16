using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.Email))
                .MaximumLength(200);
            RuleFor(x => x.Dto.Phone).MaximumLength(50);
        }
    }
}
