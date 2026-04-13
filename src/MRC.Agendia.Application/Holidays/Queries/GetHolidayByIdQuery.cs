using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public record GetHolidayByIdQuery(int Id) : IRequest<HolidayCalendarDto?>;
}
