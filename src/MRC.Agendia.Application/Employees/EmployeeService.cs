using AutoMapper;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Employees
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public EmployeeService(IEmployeeRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<PagedResult<EmployeeDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<EmployeeDto>>(items);
            return PagedResult<EmployeeDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<PagedResult<EmployeeDto>> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedByOwnerUserIdAsync(ownerUserId, page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<EmployeeDto>>(items);
            return PagedResult<EmployeeDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<EmployeeDto>(entity);
        }

        public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<Employee>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<EmployeeDto>(entity);
        }

        public async Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new EmployeeNotFoundException(dto.Id);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<EmployeeDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new EmployeeNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }
        #endregion CRUD
    }
}
