using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Appointments.Queries.MyAppointments
{
    /// <summary>
    /// Returns the paged list of appointments belonging to the currently
    /// authenticated Client. The client identity is resolved from the JWT
    /// in the handler, never from the request payload.
    /// </summary>
    public record GetMyAppointmentsAsClientQuery(
        int Page = PaginationConstants.DefaultPage,
        int PageSize = PaginationConstants.DefaultPageSize)
        : IRequest<PagedResult<AppointmentDto>>;
}
