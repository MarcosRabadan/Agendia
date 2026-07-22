using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Create
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
            RuleFor(x => x.Dto.UserId).MaximumLength(450);
            RuleFor(x => x.Dto.MaxConcurrentAppointments)
                .InclusiveBetween(1, 100)
                .WithMessage("MaxConcurrentAppointments debe estar entre 1 y 100.");
        }
    }
}
