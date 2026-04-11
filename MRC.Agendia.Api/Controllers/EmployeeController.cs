using MediatR;
using Microsoft.AspNetCore.Mvc;
using MRC.Agendia.Application.Employees.Commands;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Employees.Queries;

namespace MRC.Agendia.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EmployeeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetAll()
        {
            var result = await _mediator.Send(new GetAllEmployeesQuery());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDto>> GetById(int id)
        {
            var result = await _mediator.Send(new GetEmployeeByIdQuery(id));
            if (result is null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeDto>> Create([FromBody] CreateEmployeeDto dto)
        {
            var result = await _mediator.Send(new CreateEmployeeCommand(dto));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeDto>> Update(int id, [FromBody] UpdateEmployeeDto dto)
        {
            if (id != dto.Id) return BadRequest("Id mismatch.");
            var result = await _mediator.Send(new UpdateEmployeeCommand(dto));
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _mediator.Send(new DeleteEmployeeCommand(id));
            return NoContent();
        }
    }
}
