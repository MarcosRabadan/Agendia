using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries
{
    /// <summary>
    /// Public lookup of an active business by id. Returns a customer-safe
    /// projection (<see cref="BusinessPublicDto"/>); inactive businesses
    /// resolve to <c>null</c> so the endpoint can answer with 404.
    /// </summary>
    public record GetBusinessByIdQuery(int Id) : IRequest<BusinessPublicDto?>;
}
