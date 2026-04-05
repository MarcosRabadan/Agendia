using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IBusinessScheduleRepository
    {
        Task<BusinessSchedule?> GetByIdAsync(int id);
        Task<IEnumerable<BusinessSchedule>> GetAllAsync();
        Task AddAsync(BusinessSchedule businessSchedule);
        void Update(BusinessSchedule businessSchedule);
        void Delete(BusinessSchedule businessSchedule);
    }
}
