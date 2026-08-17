using FluentValidation;
using MRC.Agendia.Application.Common;

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

            // The bounds are wall-clock times like the appointments they filter. A zoned
            // value from the query string binds as Kind=Local shifted to the server's
            // offset, which would silently answer for a different window.
            RuleFor(x => x.StartDate).MustBeWallClock();

            RuleFor(x => x.EndDate)
                .GreaterThanOrEqualTo(x => x.StartDate)
                .WithMessage("The end date must be after or equal to the start date.")
                .MustBeWallClock();

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxRangeDays)
                .WithMessage($"The range cannot exceed {MaxRangeDays} days.");
        }
    }
}
