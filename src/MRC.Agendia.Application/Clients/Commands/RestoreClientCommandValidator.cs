using FluentValidation;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class RestoreClientCommandValidator : AbstractValidator<RestoreClientCommand>
    {
        public RestoreClientCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
