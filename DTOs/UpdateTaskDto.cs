using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class UpdateTaskDto
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public DateTime? DueDate { get; set; }

        [MaxLength(20)]
        public string? Priority { get; set; }

        public int? ColumnId { get; set; }

        public int? Order { get; set; }
    }
}