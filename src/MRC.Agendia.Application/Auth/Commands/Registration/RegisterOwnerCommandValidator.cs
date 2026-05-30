using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Auth.Commands.Registration
{
    public class RegisterOwnerCommandValidator : AbstractValidator<RegisterOwnerCommand>
    {
        public RegisterOwnerCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();

            // Owner user fields
            RuleFor(x => x.Dto.Email)
                .NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Dto.Password)
                .NotEmpty().MinimumLength(8).MaximumLength(200);
            RuleFor(x => x.Dto.FullName)
                .NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Phone)
                .NotEmpty().MaximumLength(50);

            // Business fields
            RuleFor(x => x.Dto.BusinessName)
                .NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.BusinessAddress)
                .NotEmpty().MaximumLength(500);
            RuleFor(x => x.Dto.BusinessPhone)
                .NotEmpty().MaximumLength(50);
            RuleFor(x => x.Dto.BusinessEmail)
                .NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Dto.BusinessDescription)
                .MaximumLength(2000);
            RuleFor(x => x.Dto.BusinessDefaultLanguage)
                .Must(lang => SupportedLanguages.IsSupported(lang))
                .WithMessage("El idioma no esta soportado. Valores validos: es, en, fr.");
        }
    }
}
