using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments
{
    public interface IAppointmentService
    {
        Task<PagedResult<AppointmentDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<PagedResult<AppointmentDto>> GetPagedByClientUserIdAsync(string userId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto, CancellationToken cancellationToken = default);
        Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<AppointmentDto>> GetByBusinessIdAndDateRangeAsync(int businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    }
}
