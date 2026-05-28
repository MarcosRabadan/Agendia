using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Appointments.Commands;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/businesses/{businessId}/notify-delay")]
    [Produces("application/json")]
    public class DelayNotificationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DelayNotificationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Avisa a los clientes con cita proxima del mismo tramo de que el negocio
        /// va con retraso (minutos indicados). Alcance: todo el negocio o un empleado
        /// concreto. Solo personal del negocio.
        /// </summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPost]
        [ProducesResponseType(typeof(DelayNotificationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<DelayNotificationResultDto>> NotifyDelay(int businessId, [FromBody] NotifyDelayDto dto)
        {
            var result = await _mediator.Send(new NotifyDelayCommand(businessId, dto));
            return Ok(result);
        }
    }
}
