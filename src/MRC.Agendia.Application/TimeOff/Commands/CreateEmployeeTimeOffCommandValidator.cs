using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.TimeOff.Commands
{
    public class CreateEmployeeTimeOffCommandValidator : AbstractValidator<CreateEmployeeTimeOffCommand>
    {
        /// <summary>Longest single block. Anything longer belongs in the yearly schedule.</summary>
        public const int MaxDurationDays = 366;

        public const int MaxReasonLength = 200;

        public CreateEmployeeTimeOffCommandValidator()
        {
            RuleFor(x => x.EmployeeId).NotEmpty();
            RuleFor(x => x.Dto).NotNull();

            RuleFor(x => x.Dto.Start)
                .MustBeWithinSupportedDates()
                .MustBeWallClock();

            RuleFor(x => x.Dto.End)
                .MustBeWithinSupportedDates()
                .GreaterThan(x => x.Dto.Start)
                .WithMessage("End must be after Start.")
                .MustBeWallClock();

            RuleFor(x => x.Dto)
                .Must(d => (d.End - d.Start).TotalDays <= MaxDurationDays)
                .WithMessage($"A time-off block cannot span more than {MaxDurationDays} days.");

            RuleFor(x => x.Dto.Reason)
                .MaximumLength(MaxReasonLength)
                .When(x => x.Dto.Reason is not null);
        }
    }
}
