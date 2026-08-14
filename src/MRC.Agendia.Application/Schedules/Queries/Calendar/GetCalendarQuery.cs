using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Calendar
{
    public record GetCalendarQuery(Guid BusinessId, DateOnly From, DateOnly To) : IRequest<IEnumerable<CalendarDayDto>>;
}
