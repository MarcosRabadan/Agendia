using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class UpdateBusinessScheduleCommandHandler : IRequestHandler<UpdateBusinessScheduleCommand, BusinessScheduleDto>
    {
        private readonly IBusinessScheduleService _service;

        public UpdateBusinessScheduleCommandHandler(IBusinessScheduleService service)
        {
            _service = service;
        }

        public async Task<BusinessScheduleDto> Handle(UpdateBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
