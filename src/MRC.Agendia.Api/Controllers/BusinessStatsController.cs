using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Application.Statistics.Queries;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId:int}/stats")]
    [Produces("application/json")]
    public class BusinessStatsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessStatsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Estadisticas del negocio en un rango de fechas: reservas por mes y semana,
        /// servicios mas y menos usados (con ingresos), ausencias y cancelaciones,
        /// e ingresos por hora y por dia de la semana. Solo el dueno (o un admin).
        /// </summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpGet]
        [ProducesResponseType(typeof(BusinessStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BusinessStatsDto>> GetStats(
            int businessId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            var result = await _mediator.Send(new GetBusinessStatsQuery(businessId, from, to));
            return Ok(result);
        }
    }
}
