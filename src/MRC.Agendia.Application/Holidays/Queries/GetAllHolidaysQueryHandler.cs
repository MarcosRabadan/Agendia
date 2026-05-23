using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public class GetAllHolidaysQueryHandler : IRequestHandler<GetAllHolidaysQuery, IEnumerable<HolidayCalendarDto>>
    {
        private readonly IHolidayService _service;

        public GetAllHolidaysQueryHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<HolidayCalendarDto>> Handle(GetAllHolidaysQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetAllAsync(cancellationToken);
        }
    }
}
