using System.ComponentModel.DataAnnotations;

namespace GliderView.API.Models
{
    public class UpdatePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
    }
}
