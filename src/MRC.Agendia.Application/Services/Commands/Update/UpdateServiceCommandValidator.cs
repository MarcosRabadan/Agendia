using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands.Update
{
    public class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
    {
        public UpdateServiceCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).NotEmpty();
            RuleFor(x => x.Dto.DurationMinutes)
                .GreaterThan(0).WithMessage("DurationMinutes must be greater than 0.")
                .LessThanOrEqualTo(24 * 60).WithMessage("DurationMinutes cannot exceed 24 hours.");
        }
    }
}
