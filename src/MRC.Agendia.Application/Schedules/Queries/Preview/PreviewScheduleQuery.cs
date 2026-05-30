using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Preview
{
    public record PreviewScheduleQuery(GenerateScheduleRequestDto Dto) : IRequest<IEnumerable<CalendarDayDto>>;
}
