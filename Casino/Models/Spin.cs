using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Casino.Models
{
    public class Spin
    {
        public long Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public IdentityUser? User { get; set; }

        public long Bet { get; set; }

        public long Win { get; set; }

        // Stores the resulting reel positions/symbols.
        // We'll improve this once we've designed the slot engine.
        public string Result { get; set; } = string.Empty;

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}