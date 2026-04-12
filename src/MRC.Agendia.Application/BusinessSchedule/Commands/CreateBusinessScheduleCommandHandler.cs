using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class CreateBusinessScheduleCommandHandler : IRequestHandler<CreateBusinessScheduleCommand, BusinessScheduleDto>
    {
        private readonly IBusinessScheduleService _service;

        public CreateBusinessScheduleCommandHandler(IBusinessScheduleService service)
        {
            _service = service;
        }

        public async Task<BusinessScheduleDto> Handle(CreateBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto);
        }
    }
}
