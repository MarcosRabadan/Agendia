using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetCalendarQueryHandler : IRequestHandler<GetCalendarQuery, IEnumerable<CalendarDayDto>>
    {
        private readonly IScheduleService _service;

        public GetCalendarQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<CalendarDayDto>> Handle(GetCalendarQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetCalendarAsync(request.BusinessId, request.From, request.To, cancellationToken);
        }
    }
}
