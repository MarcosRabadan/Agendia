using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments
{
    public interface IAppointmentService
    {
        Task<IEnumerable<AppointmentDto>> GetAllAsync();
        Task<AppointmentDto?> GetByIdAsync(int id);
        Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);
        Task<AppointmentDto> UpdateAsync(UpdateAppointmentDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
