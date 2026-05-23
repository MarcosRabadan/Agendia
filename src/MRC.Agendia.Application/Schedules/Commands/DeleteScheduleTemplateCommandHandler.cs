using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class DeleteScheduleTemplateCommandHandler : IRequestHandler<DeleteScheduleTemplateCommand, bool>
    {
        private readonly IScheduleService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteScheduleTemplateCommandHandler(IScheduleService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageScheduleTemplateAsync(request.Id, cancellationToken);
            return await _service.DeleteTemplateAsync(request.Id, cancellationToken);
        }
    }
}
