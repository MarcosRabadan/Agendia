using FluentValidation;

namespace MRC.Agendia.Application.Business.Commands.Delete
{
    public class DeleteBusinessCommandValidator : AbstractValidator<DeleteBusinessCommand>
    {
        public DeleteBusinessCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
