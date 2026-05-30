using FluentValidation;

namespace MRC.Agendia.Application.Business.Commands.Restore
{
    public class RestoreBusinessCommandValidator : AbstractValidator<RestoreBusinessCommand>
    {
        public RestoreBusinessCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
