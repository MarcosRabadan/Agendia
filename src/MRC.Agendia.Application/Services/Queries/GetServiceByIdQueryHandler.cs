using MediatR;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Queries
{
    public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto?>
    {
        private readonly IServicesService _service;

        public GetServiceByIdQueryHandler(IServicesService service)
        {
            _service = service;
        }

        public async Task<ServiceDto?> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id, cancellationToken);
        }
    }
}
