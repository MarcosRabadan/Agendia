using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Overrides
{
    public class UpdateScheduleOverrideCommandHandler : IRequestHandler<UpdateScheduleOverrideCommand, ScheduleOverrideDto>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateScheduleOverrideCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ScheduleOverrideDto> Handle(UpdateScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageScheduleOverrideAsync(request.Dto.Id, cancellationToken);
            return await _service.UpdateOverrideAsync(request.Dto, cancellationToken);
        }
    }
}
