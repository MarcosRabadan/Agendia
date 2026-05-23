using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class RestoreAppointmentCommandValidator : AbstractValidator<RestoreAppointmentCommand>
    {
        public RestoreAppointmentCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
