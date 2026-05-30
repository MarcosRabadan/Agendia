using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands.Series
{
    public class DeleteAppointmentSeriesCommandValidator : AbstractValidator<DeleteAppointmentSeriesCommand>
    {
        public DeleteAppointmentSeriesCommandValidator()
        {
            RuleFor(x => x.SeriesId).NotEqual(Guid.Empty);
        }
    }
}
