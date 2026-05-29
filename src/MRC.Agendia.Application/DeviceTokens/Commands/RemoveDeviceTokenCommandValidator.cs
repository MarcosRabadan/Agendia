using FluentValidation;

namespace MRC.Agendia.Application.DeviceTokens.Commands
{
    public class RemoveDeviceTokenCommandValidator : AbstractValidator<RemoveDeviceTokenCommand>
    {
        public RemoveDeviceTokenCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Token).NotEmpty().MaximumLength(450);
        }
    }
}
