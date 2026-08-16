using FluentValidation;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.Commands.CancellationPolicy
{
    /// <summary>
    /// Keeps a saved policy total and unambiguous: the tiers must cover every moment
    /// (a 0-hour tier is always present), no two may share a threshold, and the penalty
    /// value must match the kind.
    /// </summary>
    public class UpdateCancellationPolicyCommandValidator : AbstractValidator<UpdateCancellationPolicyCommand>
    {
        /// <summary>A year of notice: past that a "tier" stops meaning anything.</summary>
        public const int MaxHoursBefore = 8760;

        public UpdateCancellationPolicyCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Tiers).NotNull();

            RuleForEach(x => x.Dto.Tiers).ChildRules(tier =>
            {
                tier.RuleFor(t => t.MinHoursBefore)
                    .InclusiveBetween(0, MaxHoursBefore)
                    .WithMessage($"The advance notice of a tier must be between 0 and {MaxHoursBefore} hours.");

                // A percentage is a share of the service price; an amount is money.
                tier.RuleFor(t => t.PenaltyValue)
                    .NotNull().InclusiveBetween(0.01m, 100m)
                    .When(t => t.PenaltyKind == CancellationPenaltyKind.Percentage)
                    .WithMessage("A percentage penalty must be between 0.01 and 100.");

                tier.RuleFor(t => t.PenaltyValue)
                    .NotNull().GreaterThan(0)
                    .When(t => t.PenaltyKind == CancellationPenaltyKind.FixedAmount)
                    .WithMessage("A fixed-amount penalty must be greater than 0.");

                tier.RuleFor(t => t.PenaltyValue)
                    .Null()
                    .When(t => t.PenaltyKind is CancellationPenaltyKind.None or CancellationPenaltyKind.NotAllowed)
                    .WithMessage("A free or blocked tier cannot carry a penalty value.");
            });

            // An empty list is valid: it clears the tiers and the business falls back to
            // its single CancellationWindowHours threshold.
            When(x => x.Dto.Tiers.Count > 0, () =>
            {
                RuleFor(x => x.Dto.Tiers)
                    .Must(tiers => tiers.Select(t => t.MinHoursBefore).Distinct().Count() == tiers.Count)
                    .WithMessage("Two tiers cannot share the same advance notice.");

                RuleFor(x => x.Dto.Tiers)
                    .Must(tiers => tiers.Any(t => t.MinHoursBefore == 0))
                    .WithMessage("The policy must include a 0-hour tier so every cancellation falls in one.");
            });
        }
    }
}
