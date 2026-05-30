using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Services.Commands.Create;
using MRC.Agendia.Application.Services.Commands.Delete;
using MRC.Agendia.Application.Services.Commands.Restore;
using MRC.Agendia.Application.Services.Commands.Update;
using MRC.Agendia.Application.Services.Queries.GetAll;
using MRC.Agendia.Application.Services.Queries.GetById;

namespace MRC.Agendia.Api.Controllers
{
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

        /// <summary>Gets a paged list of services.</summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<ServiceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<ServiceDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllServicesQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>Gets a service by its identifier.</summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetServiceByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        /// <summary>Creates a new service.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPost]
        [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ServiceDto>> Create([FromBody] CreateServiceDto dto)
        {
            var result = await _mediator.Send(new CreateServiceCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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
