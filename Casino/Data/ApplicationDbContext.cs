using Casino.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Casino.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<PlayerAccount> PlayerAccounts { get; set; }
        public DbSet<Spin> Spins { get; set; }



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PlayerAccount>()
                .HasIndex(p => p.UserId)
                .IsUnique();
        }
    }
}
