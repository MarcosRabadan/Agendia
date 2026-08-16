using FluentValidation;

namespace MRC.Agendia.Application.Availability.Queries
{
    public class GetAvailabilityQueryValidator : AbstractValidator<GetAvailabilityQuery>
    {
        public GetAvailabilityQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.EmployeeId)
                .NotEqual(Guid.Empty).When(x => x.EmployeeId.HasValue);
            RuleFor(x => x.Date)
                .NotEqual(default(DateOnly))
                .WithMessage("Date is required.");
            RuleFor(x => x.StepMinutes)
                .InclusiveBetween(5, 120)
                .WithMessage("StepMinutes must be between 5 and 120.");
            When(x => x.ExtraServiceIds is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.ExtraServiceIds).NotEmpty();
                RuleFor(x => x.ExtraServiceIds!.Count)
                    .LessThanOrEqualTo(10)
                    .WithMessage("No more than 10 extra services can be combined.");
                RuleFor(x => x)
                    .Must(q => q.ExtraServiceIds!.Distinct().Count() == q.ExtraServiceIds!.Count
                               && !q.ExtraServiceIds!.Contains(q.ServiceId))
                    .WithMessage("Extra services cannot be repeated nor match the primary service.");
            });
        }
    }
}
