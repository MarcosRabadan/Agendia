using FluentValidation;

namespace MRC.Agendia.Application.DeviceTokens.Commands
{
    public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
    {
        public RegisterDeviceTokenCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Token).NotEmpty().MaximumLength(450);
            RuleFor(x => x.Dto.Platform).IsInEnum();
        }
    }
}
