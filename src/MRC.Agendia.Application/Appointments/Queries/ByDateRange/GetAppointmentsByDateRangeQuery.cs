using MediatR;
using MRC.Agendia.Application.Appointments.DTO;

namespace MRC.Agendia.Application.Appointments.Queries.ByDateRange
{
    public record GetAppointmentsByDateRangeQuery(
        Guid BusinessId,
        DateTime StartDate,
        DateTime EndDate) : IRequest<IEnumerable<AppointmentDto>>;
}
