using System.ComponentModel.DataAnnotations;

namespace GliderView.API.Models
{
    public class CreateUserDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string? EmailAddress { get; set; }

        [Required]
        [MaxLength(255)]
        public string? Name { get; set; }

        [Required]
        public char? Role { get; set; }
    }
}
