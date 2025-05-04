using Microsoft.EntityFrameworkCore;
using AICourseTester.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AICourseTester.Data
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
                .HasDefaultValue(true);

            modelBuilder.Entity<AlphaBeta>()
                .Property(fp => fp.TreeHeight)
                .HasDefaultValue(3);

            modelBuilder.Entity<AlphaBeta>()
                .Property(fp => fp.IsSolved)
                .HasDefaultValue(true);
        }
    }
}
