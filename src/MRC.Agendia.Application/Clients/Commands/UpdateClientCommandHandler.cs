using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientDto>
    {
        private readonly IClientRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ClientDto> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"Client with Id {request.Dto.Id} not found.");

            entity.Name = request.Dto.Name;
            entity.Phone = request.Dto.Phone;
            entity.Email = request.Dto.Email;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new ClientDto(entity.Id, entity.Name, entity.Phone, entity.Email);
        }
    }
}
