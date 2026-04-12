using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule
{
    public interface IBusinessScheduleService
    {
        Task<IEnumerable<BusinessScheduleDto>> GetAllAsync();
        Task<BusinessScheduleDto?> GetByIdAsync(int id);
        Task<BusinessScheduleDto> CreateAsync(CreateBusinessScheduleDto dto);
        Task<BusinessScheduleDto> UpdateAsync(UpdateBusinessScheduleDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
