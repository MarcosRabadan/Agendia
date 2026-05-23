using AutoMapper;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Clients
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ClientService(IClientRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<PagedResult<ClientDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<ClientDto>>(items);
            return PagedResult<ClientDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<ClientDto>(entity);
        }

        public async Task<ClientDto> CreateAsync(CreateClientDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<Client>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<ClientDto>(entity);
        }

        public async Task<ClientDto> UpdateAsync(UpdateClientDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new ClientNotFoundException(dto.Id);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<ClientDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ClientNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }

        #endregion CRUD

    }
}
