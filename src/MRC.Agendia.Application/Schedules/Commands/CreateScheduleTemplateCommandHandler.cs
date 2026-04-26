using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class CreateScheduleTemplateCommandHandler : IRequestHandler<CreateScheduleTemplateCommand, ScheduleTemplateDto>
    {
        private readonly IScheduleService _service;

        public CreateScheduleTemplateCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleTemplateDto> Handle(CreateScheduleTemplateCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateTemplateAsync(request.Dto);
        }
    }
}
