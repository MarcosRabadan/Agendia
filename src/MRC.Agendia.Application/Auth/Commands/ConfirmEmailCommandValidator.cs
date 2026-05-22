using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
    {
        public ConfirmEmailCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.UserId)
                .NotEmpty().WithMessage("UserId es obligatorio.");
            RuleFor(x => x.Dto.Token)
                .NotEmpty().WithMessage("Token es obligatorio.");
        }
    }
}
