namespace ProjectManagementBackend.DTOs
{
    public class TaskAssignmentDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}