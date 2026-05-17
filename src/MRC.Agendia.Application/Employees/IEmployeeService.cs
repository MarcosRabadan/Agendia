using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees
{
    public interface IEmployeeService
    {
        Task<PagedResult<EmployeeDto>> GetPagedAsync(int page, int pageSize);
        Task<PagedResult<EmployeeDto>> GetPagedByOwnerUserIdAsync(string ownerUserId, int page, int pageSize);
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
        Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
