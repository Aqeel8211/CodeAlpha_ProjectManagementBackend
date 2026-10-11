using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class CommentService
    {
        private readonly AppDbContext _context;

        public CommentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CommentDto>> GetCommentsAsync(int taskId, int userId)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            return comments.Select(MapToDto).ToList();
        }

        public async Task<CommentDto> AddCommentAsync(int taskId, int userId, CreateCommentDto dto)
        {
            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null) throw new Exception("Task not found.");

            await EnsureProjectMemberAsync(task.ProjectId, userId);

            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            var comment = new Comment
            {
                TaskId = taskId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            comment.User = user;
            return MapToDto(comment);
        }

        public async Task<CommentDto> UpdateCommentAsync(int commentId, int userId, UpdateCommentDto dto)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null) throw new Exception("Comment not found.");

            if (comment.UserId != userId)
                throw new Exception("You can only edit your own comments.");

            comment.Content = dto.Content;
            await _context.SaveChangesAsync();

            return MapToDto(comment);
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null) throw new Exception("Comment not found.");

            if (comment.UserId != userId)
                throw new Exception("You can only delete your own comments.");

            _context.Comments.Remove(comment);
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

        private static CommentDto MapToDto(Comment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                Username = comment.User?.Username ?? string.Empty,
                ProfilePictureUrl = comment.User?.ProfilePictureUrl,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt
            };
        }
    }
}