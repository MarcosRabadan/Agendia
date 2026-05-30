using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands.Registration
{
    public class RegisterClientCommandValidator : AbstractValidator<RegisterClientCommand>
    {
        public RegisterClientCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Email)
                .NotEmpty().WithMessage("Email es obligatorio.")
                .EmailAddress().WithMessage("Email no tiene formato valido.")
                .MaximumLength(200);
            RuleFor(x => x.Dto.Password)
                .NotEmpty().WithMessage("Password es obligatoria.")
                .MinimumLength(8).WithMessage("Password debe tener al menos 8 caracteres.")
                .MaximumLength(200);
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().WithMessage("FullName es obligatorio.")
                .MaximumLength(200);
            RuleFor(x => x.Dto.Phone)
                .NotEmpty().WithMessage("Phone es obligatorio.")
                .MaximumLength(50);
        }
    }
}
