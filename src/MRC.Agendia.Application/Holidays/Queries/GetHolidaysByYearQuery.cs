using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public record GetHolidaysByYearQuery(int Year, string? Region) : IRequest<IEnumerable<HolidayCalendarDto>>;
}
