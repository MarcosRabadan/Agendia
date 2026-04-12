using MediatR;
using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Application.Business.Queries
{
    public record GetBusinessByIdQuery(int Id) : IRequest<BusinessDto?>;
}
