using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Holidays.DTO
{
    public record HolidayCalendarDto(
        Guid Id,
        DateOnly Date,
        string Name,
        HolidayScope Scope,
        int Year);
}
