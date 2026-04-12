using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetAllBusinessesQueryHandler : IRequestHandler<GetAllBusinessesQuery, IEnumerable<BusinessDto>>
    {
        private readonly IBusinessService _service;

        public GetAllBusinessesQueryHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<BusinessDto>> Handle(GetAllBusinessesQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetAllAsync();
        }
    }
}
