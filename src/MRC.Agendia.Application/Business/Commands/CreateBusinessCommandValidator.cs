using FluentValidation;

namespace MRC.Agendia.Application.Business.Commands
{
    public class CreateBusinessCommandValidator : AbstractValidator<CreateBusinessCommand>
    {
        public CreateBusinessCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Address).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Dto.Phone).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Dto.Description).MaximumLength(2000);
        }
    }
}
