using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class ProjectService
    {
        private readonly AppDbContext _context;

        public ProjectService(AppDbContext context)
        {
            _context = context;
        }

        // Get all projects the user owns or is a member of
        public async Task<List<ProjectDto>> GetMyProjectsAsync(int userId)
        {
            var projects = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return projects.Select(p => MapToDto(p, userId)).ToList();
        }

        // Get a project's full details
        public async Task<ProjectDetailDto?> GetProjectByIdAsync(int projectId, int userId)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members).ThenInclude(m => m.User)
                .Include(p => p.Columns).ThenInclude(c => c.Tasks).ThenInclude(t => t.Assignments).ThenInclude(a => a.User)
                .Include(p => p.Columns).ThenInclude(c => c.Tasks).ThenInclude(t => t.Comments)
                .Include(p => p.Columns).ThenInclude(c => c.Tasks).ThenInclude(t => t.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) return null;

            var isMember = project.OwnerId == userId || project.Members.Any(m => m.UserId == userId);
            if (!isMember) throw new Exception("You are not a member of this project.");

            var myRole = project.OwnerId == userId
                ? "Admin"
                : project.Members.First(m => m.UserId == userId).Role;

            return new ProjectDetailDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                OwnerId = project.OwnerId,
                OwnerUsername = project.Owner.Username,
                CreatedAt = project.CreatedAt,
                MyRole = myRole,
                Members = project.Members.Select(m => new ProjectMemberDto
                {
                    UserId = m.UserId,
                    Username = m.User.Username,
                    ProfilePictureUrl = m.User.ProfilePictureUrl,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                }).ToList(),
                Columns = project.Columns
                    .OrderBy(c => c.Order)
                    .Select(c => new ColumnDto
                    {
                        Id = c.Id,
                        ProjectId = c.ProjectId,
                        Name = c.Name,
                        Order = c.Order,
                        Tasks = c.Tasks
                            .OrderBy(t => t.Order)
                            .Select(t => new TaskDto
                            {
                                Id = t.Id,
                                ProjectId = t.ProjectId,
                                ColumnId = t.ColumnId,
                                Title = t.Title,
                                Description = t.Description,
                                DueDate = t.DueDate,
                                Priority = t.Priority,
                                Order = t.Order,
                                CreatedById = t.CreatedById,
                                CreatedByUsername = t.CreatedBy?.Username ?? string.Empty,
                                CreatedAt = t.CreatedAt,
                                CommentCount = t.Comments?.Count ?? 0,
                                Assignments = t.Assignments.Select(a => new TaskAssignmentDto
                                {
                                    UserId = a.UserId,
                                    Username = a.User.Username,
                                    ProfilePictureUrl = a.User.ProfilePictureUrl,
                                    AssignedAt = a.AssignedAt
                                }).ToList()
                            }).ToList()
                    }).ToList()
            };
        }

        public async Task<ProjectDto> CreateProjectAsync(int userId, CreateProjectDto dto)
        {
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Add default columns
            var defaultColumns = new List<BoardColumn>
            {
                new BoardColumn { ProjectId = project.Id, Name = "To Do", Order = 0 },
                new BoardColumn { ProjectId = project.Id, Name = "In Progress", Order = 1 },
                new BoardColumn { ProjectId = project.Id, Name = "Done", Order = 2 }
            };
            _context.BoardColumns.AddRange(defaultColumns);
            await _context.SaveChangesAsync();

            var created = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .FirstAsync(p => p.Id == project.Id);

            return MapToDto(created, userId);
        }

        public async Task<ProjectDto> UpdateProjectAsync(int projectId, int userId, UpdateProjectDto dto)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.Members)
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) throw new Exception("Project not found.");

            var isOwner = project.OwnerId == userId;
            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == "Admin");
            if (!isOwner && !isAdmin)
                throw new Exception("You are not allowed to edit this project.");

            project.Name = dto.Name;
            project.Description = dto.Description;
            await _context.SaveChangesAsync();

            return MapToDto(project, userId);
        }

        public async Task DeleteProjectAsync(int projectId, int userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new Exception("Project not found.");

            if (project.OwnerId != userId)
                throw new Exception("Only the owner can delete this project.");

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }

        public async Task<ProjectMemberDto> AddMemberAsync(int projectId, int userId, AddProjectMemberDto dto)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) throw new Exception("Project not found.");

            var isOwner = project.OwnerId == userId;
            var isAdmin = project.Members.Any(m => m.UserId == userId && m.Role == "Admin");
            if (!isOwner && !isAdmin)
                throw new Exception("You are not allowed to add members.");

            var userToAdd = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (userToAdd == null)
                throw new Exception("No user found with that email.");

            if (project.OwnerId == userToAdd.Id)
                throw new Exception("The owner is already part of this project.");

            var alreadyMember = project.Members.Any(m => m.UserId == userToAdd.Id);
            if (alreadyMember)
                throw new Exception("User is already a member of this project.");

            var role = (dto.Role == "Admin") ? "Admin" : "Member";

            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userToAdd.Id,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();

            return new ProjectMemberDto
            {
                UserId = userToAdd.Id,
                Username = userToAdd.Username,
                ProfilePictureUrl = userToAdd.ProfilePictureUrl,
                Role = role,
                JoinedAt = member.JoinedAt
            };
        }

        public async Task RemoveMemberAsync(int projectId, int userId, int memberUserId)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null) throw new Exception("Project not found.");

            if (project.OwnerId == memberUserId)
                throw new Exception("Cannot remove the project owner.");

            var isOwner = project.OwnerId == userId;
            var isSelf = userId == memberUserId;
            if (!isOwner && !isSelf)
                throw new Exception("You are not allowed to remove this member.");

            var member = project.Members.FirstOrDefault(m => m.UserId == memberUserId);
            if (member == null) throw new Exception("Member not found.");

            _context.ProjectMembers.Remove(member);
            await _context.SaveChangesAsync();
        }

        private static ProjectDto MapToDto(Project project, int userId)
        {
            var myRole = project.OwnerId == userId
                ? "Admin"
                : project.Members.FirstOrDefault(m => m.UserId == userId)?.Role ?? "Member";

            return new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                OwnerId = project.OwnerId,
                OwnerUsername = project.Owner?.Username ?? string.Empty,
                CreatedAt = project.CreatedAt,
                MemberCount = (project.Members?.Count ?? 0) + 1,
                TaskCount = project.Tasks?.Count ?? 0,
                MyRole = myRole
            };
        }
    }
}