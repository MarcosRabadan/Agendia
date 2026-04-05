using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients.Commands
{
    public record CreateClientCommand(CreateClientDto Dto) : IRequest<ClientDto>;
    public record UpdateClientCommand(UpdateClientDto Dto) : IRequest<ClientDto>;
    public record DeleteClientCommand(int Id) : IRequest<bool>;

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

    public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand, bool>
    {
        private readonly IClientRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteClientCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"Client with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
