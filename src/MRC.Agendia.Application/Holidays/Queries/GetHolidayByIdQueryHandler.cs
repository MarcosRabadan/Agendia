using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public class GetHolidayByIdQueryHandler : IRequestHandler<GetHolidayByIdQuery, HolidayCalendarDto?>
    {
        private readonly IHolidayService _service;

        public GetHolidayByIdQueryHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<HolidayCalendarDto?> Handle(GetHolidayByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
