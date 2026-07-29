using FluentValidation;

namespace MRC.Agendia.Application.ServiceAuth.Commands
{
    /// <summary>
    /// Shape validation only: a missing clientId or secret is a malformed request
    /// (400). Whether the credentials are actually valid is decided later by the
    /// authenticator and surfaces as 401, never revealing which part was wrong.
    /// </summary>
    public class AuthenticateServiceCommandValidator : AbstractValidator<AuthenticateServiceCommand>
    {
        public AuthenticateServiceCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.ClientSecret).NotEmpty().MaximumLength(500);
        }
    }
}
