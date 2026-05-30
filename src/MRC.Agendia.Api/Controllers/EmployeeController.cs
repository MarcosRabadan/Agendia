using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Application.Employees.Commands.Create;
using MRC.Agendia.Application.Employees.Commands.Delete;
using MRC.Agendia.Application.Employees.Commands.Restore;
using MRC.Agendia.Application.Employees.Commands.Update;
using MRC.Agendia.Application.Employees.Queries.GetAll;
using MRC.Agendia.Application.Employees.Queries.GetById;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EmployeeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Gets a paged list of employees.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PagedResult<EmployeeDto>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var result = await _mediator.Send(new GetAllEmployeesQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>Gets an employee by its identifier.</summary>
        [Authorize]
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetEmployeeByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        /// <summary>Creates a new employee.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpPost]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeDto dto)
        {
            var result = await _mediator.Send(new CreateEmployeeCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Updates an existing employee.</summary>
        [Authorize(Roles = RolePolicies.Staff)]
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateEmployeeCommand(dto));
            return Ok(result);
        }

        /// <summary>Soft-deletes an employee by its identifier.</summary>
        [Authorize(Roles = RolePolicies.AdminOrOwner)]
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteEmployeeCommand(id));
            return NoContent();
        }

        /// <summary>Restores a previously soft-deleted employee. Admin only.</summary>
        [Authorize(Roles = Roles.Admin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Restore(int id)
        {
            await _mediator.Send(new RestoreEmployeeCommand(id));
            return NoContent();
        }
    }
}
