using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) throw new Exception("User not found.");

            user.Bio = dto.Bio;
            user.ProfilePictureUrl = dto.ProfilePictureUrl;
            await _context.SaveChangesAsync();

            return new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<List<object>> SearchUsersAsync(string query)
        {
            var users = await _context.Users
                .Where(u => u.Username.ToLower().Contains(query.ToLower())
                         || u.Email.ToLower().Contains(query.ToLower()))
                .OrderBy(u => u.Username)
                .Take(20)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.ProfilePictureUrl
                })
                .ToListAsync();

            return users.Cast<object>().ToList();
        }
    }
}