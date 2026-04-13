using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public record GetCalendarQuery(int BusinessId, DateOnly From, DateOnly To) : IRequest<IEnumerable<CalendarDayDto>>;
}
