using FluentValidation;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CreateAppointmentSeriesCommandValidator : AbstractValidator<CreateAppointmentSeriesCommand>
    {
        private const int MaxWindowDays = 366;
        private const int MaxInterval = 52;

        public CreateAppointmentSeriesCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientId).GreaterThan(0);
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0);
            RuleFor(x => x.Dto.ServiceId).GreaterThan(0);
            RuleFor(x => x.Dto.Frequency).IsInEnum();
            RuleFor(x => x.Dto.Interval)
                .InclusiveBetween(1, MaxInterval)
                .WithMessage($"El intervalo debe estar entre 1 y {MaxInterval}.");
            RuleFor(x => x.Dto.UntilDate)
                .GreaterThanOrEqualTo(x => x.Dto.StartDate)
                .WithMessage("UntilDate debe ser igual o posterior a StartDate.");
            RuleFor(x => x.Dto)
                .Must(d => (d.UntilDate.DayNumber - d.StartDate.DayNumber) + 1 <= MaxWindowDays)
                .WithMessage($"El rango de la serie no puede superar {MaxWindowDays} dias.");
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);

            // Weekly: at least one valid weekday.
            When(x => x.Dto.Frequency == RecurrenceFrequency.Weekly, () =>
            {
                RuleFor(x => x.Dto.DaysOfWeek)
                    .NotEmpty().WithMessage("Indique al menos un dia de la semana para una serie semanal.");
                RuleForEach(x => x.Dto.DaysOfWeek).IsInEnum();
            });

            // Monthly: a day-of-month between 1 and 31.
            When(x => x.Dto.Frequency == RecurrenceFrequency.Monthly, () =>
            {
                RuleFor(x => x.Dto.DayOfMonth)
                    .Must(d => d is >= 1 and <= 31)
                    .WithMessage("Indique un dia del mes entre 1 y 31 para una serie mensual.");
            });
        }
    }
}
