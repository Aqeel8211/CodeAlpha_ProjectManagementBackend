namespace ProjectManagementBackend.DTOs
{
    public class ColumnDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<TaskDto> Tasks { get; set; } = new();
    }
}