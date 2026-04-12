using MRC.Agendia.Application.Employees.DTO;

namespace MRC.Agendia.Application.Employees
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto);
        Task<EmployeeDto> UpdateAsync(UpdateEmployeeDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
