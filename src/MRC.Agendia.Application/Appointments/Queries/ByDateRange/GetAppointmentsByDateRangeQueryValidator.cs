using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Queries.ByDateRange
{
    public class GetAppointmentsByDateRangeQueryValidator : AbstractValidator<GetAppointmentsByDateRangeQuery>
    {
        // Bound the range so a single listing request cannot pull an unbounded
        // number of appointments. One year is generous for an agenda view.
        private const int MaxRangeDays = 366;

        public GetAppointmentsByDateRangeQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("The end date must be after or equal to the start date.");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxRangeDays)
                .WithMessage($"The range cannot exceed {MaxRangeDays} days.");
        }
    }
}
