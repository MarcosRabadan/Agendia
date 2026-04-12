using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands
{
    public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
    {
        private readonly IServicesService _service;

        public CreateServiceCommandHandler(IServicesService service)
        {
            _service = service;
        }

        public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto);
        }
    }
}
