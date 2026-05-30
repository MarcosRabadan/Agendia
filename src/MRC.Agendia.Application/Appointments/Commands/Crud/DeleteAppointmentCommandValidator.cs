using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public class DeleteAppointmentCommandValidator : AbstractValidator<DeleteAppointmentCommand>
    {
        public DeleteAppointmentCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
