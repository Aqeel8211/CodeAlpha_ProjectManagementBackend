using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class ColumnService
    {
        private readonly AppDbContext _context;

        public ColumnService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ColumnDto> CreateColumnAsync(int projectId, int userId, CreateColumnDto dto)
        {
            await EnsureCanManageAsync(projectId, userId);

            var column = new BoardColumn
            {
                ProjectId = projectId,
                Name = dto.Name,
                Order = dto.Order,
                CreatedAt = DateTime.UtcNow
            };

            _context.BoardColumns.Add(column);
            await _context.SaveChangesAsync();

            return new ColumnDto
            {
                Id = column.Id,
                ProjectId = column.ProjectId,
                Name = column.Name,
                Order = column.Order,
                Tasks = new()
            };
        }

        public async Task<ColumnDto> UpdateColumnAsync(int columnId, int userId, UpdateColumnDto dto)
        {
            var column = await _context.BoardColumns
                .Include(c => c.Tasks)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null) throw new Exception("Column not found.");

            await EnsureCanManageAsync(column.ProjectId, userId);

            column.Name = dto.Name;
            column.Order = dto.Order;
            await _context.SaveChangesAsync();

            return new ColumnDto
            {
                Id = column.Id,
                ProjectId = column.ProjectId,
                Name = column.Name,
                Order = column.Order,
                Tasks = new()
            };
        }

        public async Task DeleteColumnAsync(int columnId, int userId)
        {
            var column = await _context.BoardColumns.FindAsync(columnId);
            if (column == null) throw new Exception("Column not found.");

            await EnsureCanManageAsync(column.ProjectId, userId);

            _context.BoardColumns.Remove(column);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureCanManageAsync(int projectId, int userId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null) throw new Exception("Project not found.");

            var isOwner = project.OwnerId == userId;
            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == "Admin");
            if (!isOwner && !isAdmin)
                throw new Exception("You are not allowed to manage columns in this project.");
        }
    }
}