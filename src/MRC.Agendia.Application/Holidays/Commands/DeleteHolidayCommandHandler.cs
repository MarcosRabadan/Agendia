using MediatR;

namespace MRC.Agendia.Application.Holidays.Commands
{
    public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, bool>
    {
        private readonly IHolidayService _service;

        public DeleteHolidayCommandHandler(IHolidayService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
