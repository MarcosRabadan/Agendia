using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
    {
        public CreateAppointmentCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientId).GreaterThan(0);
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0);
            RuleFor(x => x.Dto.ServiceId).GreaterThan(0);
            RuleFor(x => x.Dto.StartDate)
                .NotEqual(default(DateTime)).WithMessage("StartDate es obligatorio.");
            RuleFor(x => x.Dto.EndDate)
                .NotEqual(default(DateTime)).WithMessage("EndDate es obligatorio.")
                .GreaterThan(x => x.Dto.StartDate)
                .WithMessage("EndDate debe ser posterior a StartDate.");
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);
        }
    }
}
