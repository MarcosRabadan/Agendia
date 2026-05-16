using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.CurrentPassword)
                .NotEmpty().WithMessage("CurrentPassword es obligatoria.");
            RuleFor(x => x.Dto.NewPassword)
                .NotEmpty().WithMessage("NewPassword es obligatoria.")
                .MinimumLength(8).WithMessage("NewPassword debe tener al menos 8 caracteres.")
                .MaximumLength(200)
                .NotEqual(x => x.Dto.CurrentPassword)
                .WithMessage("NewPassword no puede ser igual a la actual.");
        }
    }
}
