using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class UpdateScheduleTemplateCommandHandler : IRequestHandler<UpdateScheduleTemplateCommand, ScheduleTemplateDto>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateScheduleTemplateCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ScheduleTemplateDto> Handle(UpdateScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageScheduleTemplateAsync(request.Dto.Id);
            return await _service.UpdateTemplateAsync(request.Dto);
        }
    }
}
