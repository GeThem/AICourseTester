using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AICourseTester.backend.Models;

namespace AICourseTester.backend.Data
{
    [Index(nameof(UserName), IsUnique = true)]
    public class ApplicationUser : IdentityUser
    {
        public string? Group { get; set; }
    }

    public class MainDbContext : IdentityDbContext<ApplicationUser>
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        public DbSet<FifteenPuzzle> Fifteens { get; set; } = null!;
        public DbSet<AlphaBeta> AlphaBeta { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.Dimensions)
                .HasDefaultValue(4);

            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.TreeHeight)
                .HasDefaultValue(3);

            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.IsSolved)
                .HasDefaultValue(false);

            modelBuilder.Entity<AlphaBeta>()
                .Property(fp => fp.TreeHeight)
                .HasDefaultValue(3);

            modelBuilder.Entity<AlphaBeta>()
                .Property(fp => fp.IsSolved)
                .HasDefaultValue(false);
        }
    }
}
