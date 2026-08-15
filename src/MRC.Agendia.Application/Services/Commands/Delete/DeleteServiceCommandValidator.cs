using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands.Delete
{
    public class DeleteServiceCommandValidator : AbstractValidator<DeleteServiceCommand>
    {
        public DeleteServiceCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
