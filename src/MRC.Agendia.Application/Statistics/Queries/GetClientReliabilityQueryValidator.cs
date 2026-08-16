using FluentValidation;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetClientReliabilityQueryValidator : AbstractValidator<GetClientReliabilityQuery>
    {
        /// <summary>Longest window allowed, matching the business stats range.</summary>
        public const int MaxWindowDays = 366;

        public GetClientReliabilityQueryValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.ClientUserId).NotEmpty();
            RuleFor(x => x.Days)
                .InclusiveBetween(1, MaxWindowDays)
                .WithMessage($"The reliability window must be between 1 and {MaxWindowDays} days.");
        }
    }
}
