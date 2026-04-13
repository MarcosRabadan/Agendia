using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Holidays.DTO
{
    public record UpdateHolidayCalendarDto(
        int Id,
        DateOnly Date,
        string Name,
        HolidayScope Scope,
        string? Region,
        int Year);
}
