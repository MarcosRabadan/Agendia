using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public record GetAllHolidaysQuery() : IRequest<IEnumerable<HolidayCalendarDto>>;
}
