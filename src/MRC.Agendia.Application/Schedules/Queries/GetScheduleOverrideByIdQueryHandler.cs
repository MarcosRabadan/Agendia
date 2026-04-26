using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetScheduleOverrideByIdQueryHandler : IRequestHandler<GetScheduleOverrideByIdQuery, ScheduleOverrideDto?>
    {
        private readonly IScheduleService _service;

        public GetScheduleOverrideByIdQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleOverrideDto?> Handle(GetScheduleOverrideByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetOverrideByIdAsync(request.Id);
        }
    }
}
