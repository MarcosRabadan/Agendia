using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class CreateScheduleOverrideCommandHandler : IRequestHandler<CreateScheduleOverrideCommand, ScheduleOverrideDto>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateScheduleOverrideCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ScheduleOverrideDto> Handle(CreateScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId);
            return await _service.CreateOverrideAsync(request.Dto);
        }
    }
}
