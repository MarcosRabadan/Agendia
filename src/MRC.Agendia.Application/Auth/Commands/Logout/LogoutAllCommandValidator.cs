using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.Logout
{
    public class LogoutAllCommandValidator : AbstractValidator<LogoutAllCommand>
    {
        public LogoutAllCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
        }
    }
}
