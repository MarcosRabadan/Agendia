using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Clients.Commands.Create;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Clients.Queries.GetByBusiness;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:int}/clients")]
    [Produces("application/json")]
    public class BusinessClientsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessClientsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Returns a paged list of the clients owned by the business. Business staff only.</summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ClientDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<ClientDto>>> GetAll(
            int businessId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetBusinessClientsQuery(businessId, page, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Creates a client owned by the business (a walk-in/phone record with no user
        /// account). Business staff only.
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost]
        [ProducesResponseType(typeof(ClientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ClientDto>> Create(int businessId, [FromBody] CreateClientDto dto)
        {
            var result = await _mediator.Send(new CreateBusinessClientCommand(businessId, dto));
            return Created($"api/Client/{result.Id}", result);
        }
    }
}
