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
            When(x => x.ExtraServiceIds is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.ExtraServiceIds).GreaterThan(0);
                RuleFor(x => x.ExtraServiceIds!.Count)
                    .LessThanOrEqualTo(10)
                    .WithMessage("No se pueden combinar mas de 10 servicios adicionales.");
                RuleFor(x => x)
                    .Must(q => q.ExtraServiceIds!.Distinct().Count() == q.ExtraServiceIds!.Count
                               && !q.ExtraServiceIds!.Contains(q.ServiceId))
                    .WithMessage("Los servicios adicionales no pueden repetirse ni coincidir con el servicio principal.");
            });
        }
    }
}
