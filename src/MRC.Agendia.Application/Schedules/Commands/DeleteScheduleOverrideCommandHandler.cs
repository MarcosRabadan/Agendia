using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class DeleteScheduleOverrideCommandHandler : IRequestHandler<DeleteScheduleOverrideCommand, bool>
    {
        private readonly IScheduleService _service;

        public DeleteScheduleOverrideCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteOverrideAsync(request.Id);
        }
    }
}
