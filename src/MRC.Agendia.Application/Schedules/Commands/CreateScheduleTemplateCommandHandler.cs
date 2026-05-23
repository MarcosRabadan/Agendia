using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class CreateScheduleTemplateCommandHandler : IRequestHandler<CreateScheduleTemplateCommand, ScheduleTemplateDto>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateScheduleTemplateCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ScheduleTemplateDto> Handle(CreateScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId, cancellationToken);
            return await _service.CreateTemplateAsync(request.Dto, cancellationToken);
        }
    }
}
