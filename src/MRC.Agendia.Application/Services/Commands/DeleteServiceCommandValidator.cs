using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands
{
    public class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
    {
        public DeleteServiceCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
