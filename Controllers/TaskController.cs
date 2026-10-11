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
    public class TaskController : ControllerBase
    {
        private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        private int GetCurrentUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(id)) throw new UnauthorizedAccessException();
            return int.Parse(id);
        }

        [HttpPost("project/{projectId}")]
        public async Task<IActionResult> Create(int projectId, [FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var task = await _taskService.CreateTaskAsync(projectId, GetCurrentUserId(), dto);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var task = await _taskService.UpdateTaskAsync(id, GetCurrentUserId(), dto);
                return Ok(task);
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
                await _taskService.DeleteTaskAsync(id, GetCurrentUserId());
                return Ok(new { message = "Task deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/assign")]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignTaskDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var assignment = await _taskService.AssignUserAsync(id, GetCurrentUserId(), dto);
                return Ok(assignment);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/assign/{userId}")]
        public async Task<IActionResult> Unassign(int id, int userId)
        {
            try
            {
                await _taskService.UnassignUserAsync(id, GetCurrentUserId(), userId);
                return Ok(new { message = "User unassigned." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}