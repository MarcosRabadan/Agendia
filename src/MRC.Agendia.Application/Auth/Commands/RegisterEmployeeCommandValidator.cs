using FluentValidation;

namespace MRC.Agendia.Application.Auth.Commands
{
    public class RegisterEmployeeCommandValidator : AbstractValidator<RegisterEmployeeCommand>
    {
        public RegisterEmployeeCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.CurrentOwnerUserId)
                .NotEmpty().WithMessage("CurrentOwnerUserId is required.");

            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.Email)
                .NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Dto.Password)
                .NotEmpty().MinimumLength(8).MaximumLength(200);
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Phone)
                .MaximumLength(50);
        }
    }
}
