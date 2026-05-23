using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetScheduleOverridesByBusinessIdQueryHandler : IRequestHandler<GetScheduleOverridesByBusinessIdQuery, IEnumerable<ScheduleOverrideDto>>
    {
        private readonly IScheduleService _service;

        public GetScheduleOverridesByBusinessIdQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<ScheduleOverrideDto>> Handle(GetScheduleOverridesByBusinessIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetOverridesByBusinessIdAsync(request.BusinessId, request.From, request.To, cancellationToken);
        }
    }
}
