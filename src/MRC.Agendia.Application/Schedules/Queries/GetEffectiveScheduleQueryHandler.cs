using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetEffectiveScheduleQueryHandler : IRequestHandler<GetEffectiveScheduleQuery, EffectiveScheduleDto>
    {
        private readonly IScheduleService _service;

        public GetEffectiveScheduleQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<EffectiveScheduleDto> Handle(GetEffectiveScheduleQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetEffectiveScheduleAsync(request.BusinessId, request.Date);
        }
    }
}
