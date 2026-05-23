namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Serializes the validate-then-insert critical section of booking an
    /// appointment for a given employee and day, so two concurrent requests
    /// cannot both pass the capacity check and over-book the same slot.
    /// The implementation only locks for the same (employee, day); on providers
    /// without a shared database to race against (e.g. the in-memory test store)
    /// it just runs the action.
    /// </summary>
    public interface IBookingConcurrencyGuard
    {
        Task ExecuteSerializedAsync(int employeeId, DateOnly date, Func<Task> action, CancellationToken cancellationToken = default);

        Task<T> ExecuteSerializedAsync<T>(int employeeId, DateOnly date, Func<Task<T>> action, CancellationToken cancellationToken = default);
    }
}
