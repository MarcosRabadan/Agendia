using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Queries.Templates
{
    public class GetScheduleTemplateByIdQueryHandler : IRequestHandler<GetScheduleTemplateByIdQuery, ScheduleTemplateDto?>
    {
        private readonly IScheduleService _service;

        public GetScheduleTemplateByIdQueryHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleTemplateDto?> Handle(GetScheduleTemplateByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetTemplateByIdAsync(request.Id, cancellationToken);
        }
    }
}
