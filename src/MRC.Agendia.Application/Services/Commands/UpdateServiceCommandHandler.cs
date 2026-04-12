using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands
{
    public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
    {
        private readonly IServicesService _service;

        public UpdateServiceCommandHandler(IServicesService service)
        {
            _service = service;
        }

        public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
