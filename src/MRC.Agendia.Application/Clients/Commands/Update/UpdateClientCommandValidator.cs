using FluentValidation;

namespace MRC.Agendia.Application.Clients.Commands.Update
{
    public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
    {
        public UpdateClientCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Phone).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Dto.Email)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.Dto.Email))
                .MaximumLength(200);
        }
    }
}
