using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class PreviewScheduleQueryHandler : IRequestHandler<PreviewScheduleQuery, IEnumerable<CalendarDayDto>>
    {
        private readonly IScheduleGenerationService _service;
        private readonly IResourceAuthorizationService _auth;

        public PreviewScheduleQueryHandler(IScheduleGenerationService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<IEnumerable<CalendarDayDto>> Handle(PreviewScheduleQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId, cancellationToken);
            return await _service.PreviewScheduleAsync(request.Dto, cancellationToken);
        }
    }
}
