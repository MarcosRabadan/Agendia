using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email es obligatorio.")
                .EmailAddress().WithMessage("Email no tiene formato valido.")
                .MaximumLength(200);
            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password es obligatoria.")
                .MaximumLength(200);
        }
    }
}
