using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Holidays.DTO
{
    public record UpdateHolidayCalendarDto(
        Guid Id,
        DateOnly Date,
        string Name,
        HolidayScope Scope,
        int Year);
}
