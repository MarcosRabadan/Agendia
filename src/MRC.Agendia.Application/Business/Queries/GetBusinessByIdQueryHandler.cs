using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, BusinessDto?>
    {
        private readonly IBusinessService _service;

        public GetBusinessByIdQueryHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<BusinessDto?> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
