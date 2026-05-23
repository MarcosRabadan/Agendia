using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees
{
    public interface IEmployeeService
    {
        Task<PagedResult<EmployeeDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<EmployeeDto>> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<EmployeeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto, CancellationToken cancellationToken = default);
        Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
    }
}
