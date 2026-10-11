using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class AssignTaskDto
    {
        [Required]
        public int UserId { get; set; }
    }
}