using Microsoft.EntityFrameworkCore;
using ProjectManagementBackend.Data;
using ProjectManagementBackend.DTOs;
using ProjectManagementBackend.Models;

namespace ProjectManagementBackend.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthService(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingEmail = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (existingEmail)
                throw new Exception("Email is already registered.");

            var existingUsername = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());
            if (existingUsername)
                throw new Exception("Username is already taken.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email.ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = token
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (user == null)
                throw new Exception("Invalid email or password.");

            var valid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!valid)
                throw new Exception("Invalid email or password.");

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Token = token
            };
        }
    }
}