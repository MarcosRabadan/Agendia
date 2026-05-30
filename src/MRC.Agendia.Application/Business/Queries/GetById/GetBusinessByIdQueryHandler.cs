using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries.GetById
{
    public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, BusinessPublicDto?>
    {
        private readonly IBusinessService _service;

        public GetBusinessByIdQueryHandler(IBusinessService service)
        {
            _service = service;
        }

        public Task<BusinessPublicDto?> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
            => _service.GetPublicByIdAsync(request.Id, cancellationToken);
    }
}
