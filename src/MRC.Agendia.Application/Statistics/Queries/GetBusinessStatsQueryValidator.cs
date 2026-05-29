using FluentValidation;

namespace MRC.Agendia.Application.Statistics.Queries
{
    public class GetBusinessStatsQueryValidator : AbstractValidator<GetBusinessStatsQuery>
    {
        private const int MaxRangeDays = 366;

        public GetBusinessStatsQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            RuleFor(x => x.From).NotEqual(default(DateOnly));
            RuleFor(x => x.To)
                .NotEqual(default(DateOnly))
                .GreaterThanOrEqualTo(x => x.From)
                .WithMessage("To debe ser igual o posterior a From.");
            RuleFor(x => x)
                .Must(q => q.To.DayNumber - q.From.DayNumber <= MaxRangeDays)
                .WithMessage($"El rango de estadisticas no puede superar {MaxRangeDays} dias.");
        }
    }
}
