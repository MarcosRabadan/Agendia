using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
    {
        public UpdateAppointmentCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.ClientId).GreaterThan(0);
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0);
            RuleFor(x => x.Dto.ServiceId).GreaterThan(0);
            RuleFor(x => x.Dto.StartDate)
                .NotEqual(default(DateTime));
            RuleFor(x => x.Dto.EndDate)
                .NotEqual(default(DateTime))
                .GreaterThan(x => x.Dto.StartDate)
                .WithMessage("EndDate debe ser posterior a StartDate.");
            RuleFor(x => x.Dto.Status).IsInEnum();
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);
        }
    }
}
