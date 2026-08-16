using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Update
{
    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).NotEmpty();
            RuleFor(x => x.Dto.MaxConcurrentAppointments)
                .InclusiveBetween(1, 100)
                .WithMessage("MaxConcurrentAppointments must be between 1 and 100.");
        }
    }
}
