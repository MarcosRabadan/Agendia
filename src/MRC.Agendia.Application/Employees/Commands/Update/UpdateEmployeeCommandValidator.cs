using FluentValidation;

namespace MRC.Agendia.Application.Employees.Commands.Update
{
    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.MaxConcurrentAppointments)
                .InclusiveBetween(1, 100)
                .WithMessage("MaxConcurrentAppointments debe estar entre 1 y 100.");
        }
    }
}
