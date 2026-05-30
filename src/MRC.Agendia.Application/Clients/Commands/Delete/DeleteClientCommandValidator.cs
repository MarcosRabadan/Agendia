using FluentValidation;

namespace MRC.Agendia.Application.Clients.Commands.Delete
{
    public class DeleteClientCommandValidator : AbstractValidator<DeleteClientCommand>
    {
        public DeleteClientCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
