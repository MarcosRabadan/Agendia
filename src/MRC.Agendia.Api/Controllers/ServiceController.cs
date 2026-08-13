using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Services.Commands.Create;
using MRC.Agendia.Application.Services.Commands.Delete;
using MRC.Agendia.Application.Services.Commands.Restore;
using MRC.Agendia.Application.Services.Commands.Update;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Provisioning of a service's scheduling projection (its duration). Agendia does
    /// not own the service catalog (name/description/price), so there are no public
    /// reads here; the management/catalog service creates and updates the projection.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ServiceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ServiceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Creates a new service (duration projection). Provisioned by the catalog service.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPost]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceDto dto)
        {
            var result = await _mediator.Send(new CreateServiceCommand(dto));
            return Created($"/api/service/{result.Id}", result);
        }

        /// <summary>Updates an existing service.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> Update(int id, [FromBody] UpdateServiceDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateServiceCommand(dto));
            return Ok(result);
        }

        /// <summary>Soft-deletes a service by its identifier.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteServiceCommand(id));
            return NoContent();
        }

        /// <summary>Restores a previously soft-deleted service. Admin only.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreServiceCommand(id));
            return NoContent();
        }
    }
}
