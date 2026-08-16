using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Create
{
    public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
    {
        public CreateEmployeeCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).NotEmpty();
            RuleFor(x => x.Dto.UserId).MaximumLength(450);
            RuleFor(x => x.Dto.MaxConcurrentAppointments)
                .InclusiveBetween(1, 100)
                .WithMessage("MaxConcurrentAppointments must be between 1 and 100.");
        }
    }
}
