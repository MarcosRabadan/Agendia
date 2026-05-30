using FluentValidation;

namespace MRC.Agendia.Application.Holidays.Commands.Delete
{
    public class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
    {
        public DeleteHolidayCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
