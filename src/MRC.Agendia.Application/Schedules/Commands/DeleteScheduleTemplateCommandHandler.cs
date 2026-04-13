using MediatR;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class DeleteScheduleTemplateCommandHandler : IRequestHandler<DeleteScheduleTemplateCommand, bool>
    {
        private readonly IScheduleService _service;

        public DeleteScheduleTemplateCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteTemplateAsync(request.Id);
        }
    }
}
