using FluentValidation;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.Commands.Series
{
    public class CreateAppointmentSeriesCommandValidator : AbstractValidator<CreateAppointmentSeriesCommand>
    {
        private const int MaxWindowDays = 366;
        private const int MaxInterval = 52;

        public CreateAppointmentSeriesCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientUserId).NotEmpty();
            RuleFor(x => x.Dto.EmployeeId).NotEmpty();
            RuleFor(x => x.Dto.ServiceId).NotEmpty();
            RuleFor(x => x.Dto.Frequency).IsInEnum();
            RuleFor(x => x.Dto.Interval)
                .InclusiveBetween(1, MaxInterval)
                .WithMessage($"The interval must be between 1 and {MaxInterval}.");
            // Absolute bounds so the day-by-day recurrence expansion cannot overflow
            // DateOnly.MaxValue (would be a 500 instead of a 400).
            RuleFor(x => x.Dto.StartDate)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage);
            RuleFor(x => x.Dto.UntilDate)
                .InclusiveBetween(SchedulingLimits.MinDate, SchedulingLimits.MaxDate)
                .WithMessage(SchedulingLimits.OutOfRangeMessage)
                .GreaterThanOrEqualTo(x => x.Dto.StartDate)
                .WithMessage("UntilDate must be the same as or after StartDate.");
            RuleFor(x => x.Dto)
                .Must(d => (d.UntilDate.DayNumber - d.StartDate.DayNumber) + 1 <= MaxWindowDays)
                .WithMessage($"The series range cannot exceed {MaxWindowDays} days.");
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);

            // Weekly: at least one valid weekday.
            When(x => x.Dto.Frequency == RecurrenceFrequency.Weekly, () =>
            {
                RuleFor(x => x.Dto.DaysOfWeek)
                    .NotEmpty().WithMessage("Provide at least one day of the week for a weekly series.");
                RuleForEach(x => x.Dto.DaysOfWeek).IsInEnum();
            });

            // Monthly: a day-of-month between 1 and 31.
            When(x => x.Dto.Frequency == RecurrenceFrequency.Monthly, () =>
            {
                RuleFor(x => x.Dto.DayOfMonth)
                    .Must(d => d is >= 1 and <= 31)
                    .WithMessage("Provide a day of the month between 1 and 31 for a monthly series.");
            });
        }
    }
}
