using System.ComponentModel.DataAnnotations;

namespace GliderView.API.Models
{
    public class ValidateInvitationDto
    {
        [Required]
        [MaxLength(100)]
        public string? Token { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }
    }
}
