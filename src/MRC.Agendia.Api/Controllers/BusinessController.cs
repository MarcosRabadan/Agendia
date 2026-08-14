using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Business.Commands.Create;
using MRC.Agendia.Application.Business.Commands.Delete;
using MRC.Agendia.Application.Business.Commands.Restore;
using MRC.Agendia.Application.Business.Commands.Update;

namespace MRC.Agendia.Api.Controllers
{
    /// <summary>
    /// Provisioning of a business' scheduling config (Agendia does not own the business
    /// profile/catalog, so there are no public reads here). The management/identity
    /// service creates and updates the config; a business is a scheduling container.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BusinessController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Creates a new business (scheduling config). Provisioned by the management service.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BusinessDto>> Create([FromBody] CreateBusinessDto dto)
        {
            var result = await _mediator.Send(new CreateBusinessCommand(dto));
            return Created($"/api/business/{result.Id}", result);
        }

        /// <summary>Updates an existing business.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessDto>> Update(Guid id, [FromBody] UpdateBusinessDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateBusinessCommand(dto));
            return Ok(result);
        }

        /// <summary>Soft-deletes a business by its identifier.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _mediator.Send(new DeleteBusinessCommand(id));
            return NoContent();
        }

        /// <summary>Restores a previously soft-deleted business. Admin only.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(Guid id)
        {
            await _mediator.Send(new RestoreBusinessCommand(id));
            return NoContent();
        }
    }
}
