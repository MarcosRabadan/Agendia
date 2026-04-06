using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients.Commands
{
    public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, ClientDto>
    {
        private readonly IClientRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ClientDto> Handle(CreateClientCommand request, CancellationToken cancellationToken)
        {
            var entity = new Client
            {
                Name = request.Dto.Name,
                Phone = request.Dto.Phone,
                Email = request.Dto.Email
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new ClientDto(entity.Id, entity.Name, entity.Phone, entity.Email);
        }
    }
}
