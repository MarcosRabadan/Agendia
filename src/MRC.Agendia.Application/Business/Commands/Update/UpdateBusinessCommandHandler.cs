using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Commands.Update
{
    public class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, BusinessDto>
    {
        private readonly IBusinessService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateBusinessCommandHandler(IBusinessService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<BusinessDto> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessAsync(request.Dto.Id, cancellationToken);
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
