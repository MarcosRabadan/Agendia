using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Business.Commands.Create;
using MRC.Agendia.Application.Business.Commands.Delete;
using MRC.Agendia.Application.Business.Commands.Restore;
using MRC.Agendia.Application.Business.Commands.Update;
using MRC.Agendia.Application.Business.Queries.GetAll;
using MRC.Agendia.Application.Business.Queries.GetById;

namespace MRC.Agendia.Api.Controllers
{
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

        /// <summary>Gets a paged list of active businesses.</summary>
        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<BusinessPublicDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<BusinessPublicDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllBusinessesQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>Gets an active business by its identifier.</summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BusinessPublicDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessPublicDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetBusinessByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        /// <summary>Creates a new business.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BusinessDto>> Create([FromBody] CreateBusinessDto dto)
        {
            var result = await _mediator.Send(new CreateBusinessCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an existing business.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BusinessDto>> Update(int id, [FromBody] UpdateBusinessDto dto)
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
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteBusinessCommand(id));
            return NoContent();
        }

        /// <summary>Restores a previously soft-deleted business. Admin only.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreBusinessCommand(id));
            return NoContent();
        }
    }
}
