using System.ComponentModel.DataAnnotations;

namespace ProjectManagementBackend.DTOs
{
    public class UpdateProfileDto
    {
        [MaxLength(200)]
        public string? Bio { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }
}