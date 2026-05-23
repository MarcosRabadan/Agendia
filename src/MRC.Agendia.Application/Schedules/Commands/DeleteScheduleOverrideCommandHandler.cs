using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class DeleteScheduleOverrideCommandHandler : IRequestHandler<DeleteScheduleOverrideCommand, bool>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteScheduleOverrideCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageScheduleOverrideAsync(request.Id, cancellationToken);
            return await _service.DeleteOverrideAsync(request.Id, cancellationToken);
        }
    }
}
