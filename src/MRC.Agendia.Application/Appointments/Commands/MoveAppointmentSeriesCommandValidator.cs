using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class MoveAppointmentSeriesCommandValidator : AbstractValidator<MoveAppointmentSeriesCommand>
    {
        private const int MaxDayShift = 366;

        public MoveAppointmentSeriesCommandValidator()
        {
            RuleFor(x => x.SeriesId).NotEqual(Guid.Empty);
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.DayShift).InclusiveBetween(-MaxDayShift, MaxDayShift);
            RuleFor(x => x.Dto)
                .Must(d => d.NewStartTime.HasValue || d.DayShift != 0)
                .WithMessage("Indique una nueva hora o un desplazamiento de dias distinto de cero.");
        }
    }
}
