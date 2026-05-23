using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands
{
    public class RestoreServiceCommandValidator : AbstractValidator<RestoreServiceCommand>
    {
        public RestoreServiceCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
