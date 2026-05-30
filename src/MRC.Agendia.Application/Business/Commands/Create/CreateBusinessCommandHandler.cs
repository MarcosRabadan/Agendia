using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands.Create
{
    public class CreateBusinessCommandHandler : IRequestHandler<CreateBusinessCommand, BusinessDto>
    {
        private readonly IBusinessService _service;

        public CreateBusinessCommandHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<BusinessDto> Handle(CreateBusinessCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
