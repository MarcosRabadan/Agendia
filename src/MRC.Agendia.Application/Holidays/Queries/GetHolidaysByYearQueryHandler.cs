using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Queries
{
    public class GetHolidaysByYearQueryHandler : IRequestHandler<GetHolidaysByYearQuery, IEnumerable<HolidayCalendarDto>>
    {
        private readonly IHolidayService _service;

        public GetHolidaysByYearQueryHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<HolidayCalendarDto>> Handle(GetHolidaysByYearQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByYearAsync(request.Year, cancellationToken);
        }
    }
}
