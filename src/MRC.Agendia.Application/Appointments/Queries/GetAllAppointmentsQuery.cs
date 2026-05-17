using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public record GetAllAppointmentsQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<AppointmentDto>>;
}
