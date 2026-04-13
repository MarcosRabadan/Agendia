using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class UpdateScheduleTemplateCommandHandler : IRequestHandler<UpdateScheduleTemplateCommand, ScheduleTemplateDto>
    {
        private readonly IScheduleService _service;

        public UpdateScheduleTemplateCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleTemplateDto> Handle(UpdateScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateTemplateAsync(request.Dto);
        }
    }
}
