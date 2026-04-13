using MediatR;

namespace MRC.Agendia.Application.Holidays.Commands
{
    public record DeleteHolidayCommand(int Id) : IRequest<bool>;
}
