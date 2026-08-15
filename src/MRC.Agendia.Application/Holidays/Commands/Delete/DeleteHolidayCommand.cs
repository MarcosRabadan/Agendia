using MediatR;

namespace MRC.Agendia.Application.Holidays.Commands.Delete
{
    public record DeleteHolidayCommand(Guid Id) : IRequest<bool>;
}
