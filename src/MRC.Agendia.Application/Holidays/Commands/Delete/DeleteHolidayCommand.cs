using MediatR;

namespace MRC.Agendia.Application.Holidays.Commands.Delete
{
    public record DeleteHolidayCommand(int Id) : IRequest<bool>;
}
