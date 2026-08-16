using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands.Create
{
    public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
    {
        public CreateServiceCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).NotEmpty();
            RuleFor(x => x.Dto.DurationMinutes)
                .GreaterThan(0).WithMessage("DurationMinutes must be greater than 0.")
                .LessThanOrEqualTo(24 * 60).WithMessage("DurationMinutes cannot exceed 24 hours.");
        }
    }
}
