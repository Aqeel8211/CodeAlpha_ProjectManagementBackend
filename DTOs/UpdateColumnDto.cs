using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class UpdateColumnDto
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int Order { get; set; }
    }
}