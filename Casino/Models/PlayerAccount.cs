using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Casino.Models
{
    public class PlayerAccount
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public IdentityUser? User { get; set; }

        // Use whole credits rather than decimal money
        public long Credits { get; set; } = 10000;

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}