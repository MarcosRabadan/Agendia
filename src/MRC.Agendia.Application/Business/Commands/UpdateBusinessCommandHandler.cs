using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands
{
    public class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, BusinessDto>
    {
        private readonly IBusinessService _service;

        public UpdateBusinessCommandHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<BusinessDto> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
