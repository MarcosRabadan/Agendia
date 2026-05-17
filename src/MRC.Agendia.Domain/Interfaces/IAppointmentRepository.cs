using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Interfaces
{
    public interface IAppointmentRepository
    {
        Task<Appointment?> GetByIdAsync(int id);
        Task<IEnumerable<Appointment>> GetAllAsync();
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedByClientIdAsync(int clientId, int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(Appointment appointment);
        Task<IEnumerable<Appointment>> GetByBusinessIdAndDateRangeAsync(
            int businessId,
            DateTime startDate,
            DateTime endDate);
    }
}
