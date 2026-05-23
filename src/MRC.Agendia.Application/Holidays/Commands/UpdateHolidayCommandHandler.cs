using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Commands
{
    public class UpdateHolidayCommandHandler : IRequestHandler<UpdateHolidayCommand, HolidayCalendarDto>
    {
        private readonly IHolidayService _service;

        public UpdateHolidayCommandHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<HolidayCalendarDto> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
