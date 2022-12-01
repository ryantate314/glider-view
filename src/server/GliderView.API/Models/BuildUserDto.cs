using System.ComponentModel.DataAnnotations;

namespace GliderView.API.Models
{
    public class BuildUserDto : ValidateInvitationDto
    {
        [Required]
        [MaxLength(100)]
        public string? Password { get; set; }
    }
}
