namespace ProjectManagementBackend.DTOs
{
    public class ProjectMemberDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime JoinedAt { get; set; }
    }
}