using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands.Series
{
    public class CancelAppointmentSeriesCommandValidator : AbstractValidator<CancelAppointmentSeriesCommand>
    {
        public CancelAppointmentSeriesCommandValidator()
        {
            RuleFor(x => x.SeriesId).NotEqual(Guid.Empty);
        }
    }
}
