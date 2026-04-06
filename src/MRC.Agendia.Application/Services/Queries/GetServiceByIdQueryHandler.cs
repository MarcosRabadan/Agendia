using MediatR;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services.Queries
{
    public class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto?>
    {
        private readonly IServiceRepository _repository;

        public GetServiceByIdQueryHandler(IServiceRepository repository)
        {
            _repository = repository;
        }

        public async Task<ServiceDto?> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new ServiceDto(entity.Id, entity.BusinessId, entity.Name, entity.Description, entity.DurationMinutes, entity.Price);
        }
    }
}
