using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class AddProjectMemberDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "Member";
    }
}