using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.Logout
{
    public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutCommandValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty().WithMessage("RefreshToken es obligatorio.")
                .MaximumLength(500);
        }
    }
}
