using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries.GetById
{
    public record GetHolidayByIdQuery(Guid Id) : IRequest<HolidayCalendarDto?>;
}
