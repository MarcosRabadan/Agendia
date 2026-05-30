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
        /// <summary>Runs <paramref name="action"/> serialized against other bookings for the same employee and day.</summary>
        /// <param name="employeeId">Employee whose slots are being booked.</param>
        /// <param name="date">Day the booking falls on.</param>
        /// <param name="action">The validate-then-insert work to run inside the lock.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        Task ExecuteSerializedAsync(int employeeId,
                                    DateOnly date,
                                    Func<Task> action,
                                    CancellationToken cancellationToken = default);

        /// <summary>Runs <paramref name="action"/> serialized against other bookings for the same employee and day, returning its result.</summary>
        /// <typeparam name="T">Type returned by the action.</typeparam>
        /// <param name="employeeId">Employee whose slots are being booked.</param>
        /// <param name="date">Day the booking falls on.</param>
        /// <param name="action">The validate-then-insert work to run inside the lock.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The value produced by the action.</returns>
        Task<T> ExecuteSerializedAsync<T>(int employeeId,
                                          DateOnly date,
                                          Func<Task<T>> action,
                                          CancellationToken cancellationToken = default);
    }
}
