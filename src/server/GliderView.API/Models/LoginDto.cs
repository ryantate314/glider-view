using System.ComponentModel.DataAnnotations;

namespace GliderView.API.Models
{
    public class LoginDto
    {
        [Required]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [Required]
        [MaxLength(255)]
        public string? Password { get; set; }

    }
}
