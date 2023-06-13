using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public char Role { get; set; }

        [JsonIgnore]
        public string? HashedPassword { get; set; }
        public bool IsSetUp {
            get {
                return HashedPassword != null;
            }
        }

        public byte FailedLoginAttempts { get; set; }
        public bool IsLockedOut { get; set; }


        public const char ROLE_ADMIN = 'A';
    }
}
