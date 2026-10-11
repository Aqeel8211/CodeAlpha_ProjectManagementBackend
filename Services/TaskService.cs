using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class TaskService
    {
        private readonly AppDbContext _context;
        private readonly NotificationService _notificationService;

        public TaskService(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<TaskDto> CreateTaskAsync(int projectId, int userId, CreateTaskDto dto)
        {
            await EnsureProjectMemberAsync(projectId, userId);

            var columnExists = await _context.BoardColumns
                .AnyAsync(c => c.Id == dto.ColumnId && c.ProjectId == projectId);
            if (!columnExists) throw new Exception("Invalid column.");

            var task = new TaskItem
            {
                ProjectId = projectId,
                ColumnId = dto.ColumnId,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                Order = dto.Order,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            return await LoadTaskDtoAsync(task.Id, userId);
        }

        public async Task<TaskDto> UpdateTaskAsync(int taskId, int userId, UpdateTaskDto dto)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            if (dto.Title != null) task.Title = dto.Title;
            if (dto.Description != null) task.Description = dto.Description;
            if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
            if (dto.Priority != null) task.Priority = dto.Priority;
            if (dto.Order.HasValue) task.Order = dto.Order.Value;
            if (dto.ColumnId.HasValue)
            {
                var colExists = await _context.BoardColumns
                    .AnyAsync(c => c.Id == dto.ColumnId.Value && c.ProjectId == task.ProjectId);
                if (!colExists) throw new Exception("Invalid column.");
                task.ColumnId = dto.ColumnId.Value;
            }

            await _context.SaveChangesAsync();
            return await LoadTaskDtoAsync(taskId, userId);
        }

        public async Task DeleteTaskAsync(int taskId, int userId)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task<TaskAssignmentDto> AssignUserAsync(int taskId, int userId, AssignTaskDto dto)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project).ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == dto.UserId)
                || task.Project.OwnerId == dto.UserId;
            if (!isMember)
                throw new Exception("User is not a member of this project.");

            var exists = await _context.TaskAssignments
                .AnyAsync(ta => ta.TaskId == taskId && ta.UserId == dto.UserId);
            if (exists)
                throw new Exception("User is already assigned to this task.");

            var assignment = new TaskAssignment
            {
                TaskId = taskId,
                UserId = dto.UserId,
                AssignedAt = DateTime.UtcNow
            };
            _context.TaskAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Create notification
            await _notificationService.CreateAsync(
                dto.UserId,
                $"You were assigned to task: {task.Title}",
                "TaskAssigned",
                taskId
            );

            var user = await _context.Users.FindAsync(dto.UserId);

            return new TaskAssignmentDto
            {
                UserId = dto.UserId,
                Username = user?.Username ?? string.Empty,
                ProfilePictureUrl = user?.ProfilePictureUrl,
                AssignedAt = assignment.AssignedAt
            };
        }

        public async Task UnassignUserAsync(int taskId, int userId, int targetUserId)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(ta => ta.TaskId == taskId && ta.UserId == targetUserId);
            if (assignment == null) throw new Exception("Assignment not found.");

            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureProjectMemberAsync(int projectId, int userId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new Exception("Project not found.");

            var isMember = project.OwnerId == userId || project.Members.Any(m => m.UserId == userId);
            if (!isMember) throw new Exception("You are not a member of this project.");
        }

        private async Task<TaskDto> LoadTaskDtoAsync(int taskId, int userId)
        {
            var task = await _context.TaskItems
                .Include(t => t.CreatedBy)
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .Include(t => t.Comments)
                .FirstAsync(t => t.Id == taskId);

            return new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                ColumnId = task.ColumnId,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                Order = task.Order,
                CreatedById = task.CreatedById,
                CreatedByUsername = task.CreatedBy?.Username ?? string.Empty,
                CreatedAt = task.CreatedAt,
                CommentCount = task.Comments?.Count ?? 0,
                Assignments = task.Assignments.Select(a => new TaskAssignmentDto
                {
                    UserId = a.UserId,
                    Username = a.User.Username,
                    ProfilePictureUrl = a.User.ProfilePictureUrl,
                    AssignedAt = a.AssignedAt
                }).ToList()
            };
        }
    }
}