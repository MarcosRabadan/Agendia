using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
    {
        public RefreshTokenCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("RefreshToken es obligatorio.")
                .MaximumLength(500);
        }
    }
}
