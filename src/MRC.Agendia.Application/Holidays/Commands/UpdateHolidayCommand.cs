using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Commands
{
    public record UpdateHolidayCommand(UpdateHolidayCalendarDto Dto) : IRequest<HolidayCalendarDto>;
}
