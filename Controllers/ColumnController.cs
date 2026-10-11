using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Services;
using System.Security.Claims;

namespace ProjectManagementBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ColumnController : ControllerBase
    {
        private readonly ColumnService _columnService;

        public ColumnController(ColumnService columnService)
        {
            _columnService = columnService;
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException();
            return int.Parse(id);
        }

        [HttpPost("project/{projectId}")]
        public async Task<IActionResult> Create(int projectId, [FromBody] CreateColumnDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var column = await _columnService.CreateColumnAsync(projectId, GetCurrentUserId(), dto);
                return Ok(column);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateColumnDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var column = await _columnService.UpdateColumnAsync(id, GetCurrentUserId(), dto);
                return Ok(column);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _columnService.DeleteColumnAsync(id, GetCurrentUserId());
                return Ok(new { message = "Column deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}