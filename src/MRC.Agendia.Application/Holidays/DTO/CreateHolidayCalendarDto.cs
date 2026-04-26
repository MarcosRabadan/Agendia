using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Holidays.DTO
{
    public record CreateHolidayCalendarDto(
        DateOnly Date,
        string Name,
        HolidayScope Scope,
        int Year);
}
