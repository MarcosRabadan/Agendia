using FluentValidation;

namespace MRC.Agendia.Application.Business.Commands
{
    public class RestoreBusinessCommandValidator : AbstractValidator<RestoreBusinessCommand>
    {
        public RestoreBusinessCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
