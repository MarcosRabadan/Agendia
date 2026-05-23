using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries
{
    public class GetScheduleTemplatesByBusinessIdQueryHandler : IRequestHandler<GetScheduleTemplatesByBusinessIdQuery, IEnumerable<ScheduleTemplateDto>>
    {
        private readonly IScheduleService _service;

        public GetScheduleTemplatesByBusinessIdQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<ScheduleTemplateDto>> Handle(GetScheduleTemplatesByBusinessIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetTemplatesByBusinessIdAsync(request.BusinessId, cancellationToken);
        }
    }
}
