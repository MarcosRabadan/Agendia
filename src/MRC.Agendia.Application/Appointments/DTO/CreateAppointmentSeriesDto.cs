using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// Request to create a recurring series of appointments in bulk (e.g. "every
    /// Friday at 16h until 31/07"). The end of each occurrence is derived from the
    /// service duration, so only the start time is provided.
    /// </summary>
    public record CreateAppointmentSeriesDto(
        int ClientId,
        int EmployeeId,
        int ServiceId,
        TimeOnly StartTime,
        RecurrenceFrequency Frequency,
        int Interval,
        IReadOnlyList<DayOfWeek>? DaysOfWeek,
        int? DayOfMonth,
        DateOnly StartDate,
        DateOnly UntilDate,
        string? Notes);
}
