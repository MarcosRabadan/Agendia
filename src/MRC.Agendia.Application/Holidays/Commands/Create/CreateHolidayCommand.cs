using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Commands.Create
{
    public record CreateHolidayCommand(CreateHolidayCalendarDto Dto) : IRequest<HolidayCalendarDto>;
}
