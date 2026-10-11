namespace ProjectManagementBackend.DTOs
{
    public class ProjectDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerId { get; set; }
        public string OwnerUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MyRole { get; set; } = "Member";
        public List<ProjectMemberDto> Members { get; set; } = new();
        public List<ColumnDto> Columns { get; set; } = new();
    }
}