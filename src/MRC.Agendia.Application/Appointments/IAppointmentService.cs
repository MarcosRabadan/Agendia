using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments
{
    public interface IAppointmentService
    {
        Task<PagedResult<AppointmentDto>> GetPagedAsync(int page, int pageSize);
        Task<PagedResult<AppointmentDto>> GetPagedByClientUserIdAsync(string userId, int page, int pageSize);
        Task<AppointmentDto?> GetByIdAsync(int id);
        Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);
        Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate);
    }
}
