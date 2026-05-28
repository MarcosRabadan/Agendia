namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// Shift the future occurrences of a series: change the time of day
    /// (<see cref="NewStartTime"/>) and/or move every date by <see cref="DayShift"/>
    /// days (e.g. -1 to bring a Friday slot to Thursday). At least one must change.
    /// </summary>
    public record MoveAppointmentSeriesDto(
        TimeOnly? NewStartTime,
        int DayShift);
}
