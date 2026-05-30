using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email es obligatorio.")
                .EmailAddress().WithMessage("Email no tiene formato valido.")
                .MaximumLength(200);
            RuleFor(x => x.Dto.Token)
                .NotEmpty().WithMessage("Token es obligatorio.");
            RuleFor(x => x.Dto.NewPassword)
                .NotEmpty().WithMessage("NewPassword es obligatoria.")
                .MinimumLength(8).WithMessage("NewPassword debe tener al menos 8 caracteres.")
                .MaximumLength(200);
        }
    }
}
