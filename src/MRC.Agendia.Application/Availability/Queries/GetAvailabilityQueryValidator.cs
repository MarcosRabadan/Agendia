using FluentValidation;

namespace MRC.Agendia.Application.Availability.Queries
{
    public class GetAvailabilityQueryValidator : AbstractValidator<GetAvailabilityQuery>
    {
        public GetAvailabilityQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            RuleFor(x => x.ServiceId).GreaterThan(0);
            RuleFor(x => x.EmployeeId)
                .GreaterThan(0).When(x => x.EmployeeId.HasValue);
            RuleFor(x => x.Date)
                .NotEqual(default(DateOnly))
                .WithMessage("Date es obligatorio.");
            RuleFor(x => x.StepMinutes)
                .InclusiveBetween(5, 120)
                .WithMessage("StepMinutes debe estar entre 5 y 120.");
        }
    }
}
