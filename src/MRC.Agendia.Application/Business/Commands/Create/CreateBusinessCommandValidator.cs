using FluentValidation;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.Commands.Create
{
    public class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
    {
        public CreateBusinessCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.OwnerUserId)
                .NotEmpty().MaximumLength(450)
                .WithMessage("The owner's user id (OwnerUserId) is required.");
            RuleFor(x => x.Dto.CancellationWindowHours)
                .InclusiveBetween(1, 8760)
                .When(x => x.Dto.CancellationWindowHours.HasValue)
                .WithMessage("The cancellation window must be between 1 and 8760 hours; omit it (null) to apply no restriction.");
            RuleFor(x => x.Dto.DefaultLanguage)
                .Must(lang => SupportedLanguages.IsSupported(lang))
                .WithMessage("The language is not supported. Valid values: es, en, fr.");
            RuleFor(x => x.Dto.DefaultAppointmentStatus)
                .Must(s => s.IsValidInitialStatus())
                .WithMessage("The default initial status can only be Pending or Confirmed.");
        }
    }
}
