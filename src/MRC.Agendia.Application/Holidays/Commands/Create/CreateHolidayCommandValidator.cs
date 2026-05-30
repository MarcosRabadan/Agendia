using FluentValidation;

namespace MRC.Agendia.Application.Holidays.Commands.Create
{
    public class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
    {
        public CreateHolidayCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Date).NotEqual(default(DateOnly));
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Scope).IsInEnum();
            RuleFor(x => x.Dto.Year)
                .GreaterThanOrEqualTo(2000)
                .LessThanOrEqualTo(2100);
            RuleFor(x => x.Dto)
                .Must(d => d.Date.Year == d.Year)
                .WithMessage("La fecha del festivo debe pertenecer al Year indicado.");
        }
    }
}
