using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetAppointmentsByDateRangeQueryValidator : AbstractValidator<GetAppointmentsByDateRangeQuery>
    {
        // Bound the range so a single listing request cannot pull an unbounded
        // number of appointments. One year is generous for an agenda view.
        private const int MaxRangeDays = 366;

        public GetAppointmentsByDateRangeQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("La fecha final debe ser posterior o igual a la inicial.");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxRangeDays)
                .WithMessage($"El rango no puede superar {MaxRangeDays} dias.");
        }
    }
}
