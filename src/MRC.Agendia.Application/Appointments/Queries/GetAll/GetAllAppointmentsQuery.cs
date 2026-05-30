using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Appointments.Queries.GetAll
{
    public record GetAllAppointmentsQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<AppointmentDto>>;
}
