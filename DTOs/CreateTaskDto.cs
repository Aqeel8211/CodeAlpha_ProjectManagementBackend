using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        public int ColumnId { get; set; }

        public int Order { get; set; } = 0;
    }
}