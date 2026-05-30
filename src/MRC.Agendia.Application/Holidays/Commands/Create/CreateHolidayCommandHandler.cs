using MediatR;
using MRC.Agendia.Application.Holidays.DTO;

namespace MRC.Agendia.Application.Holidays.Commands.Create
{
    public class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, HolidayCalendarDto>
    {
        private readonly IHolidayService _service;

        public CreateHolidayCommandHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<HolidayCalendarDto> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
