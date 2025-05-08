using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AICourseTester.Models;

namespace AICourseTester.Data
{
    public class MainDbContext : IdentityDbContext<ApplicationUser>
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        public DbSet<FifteenPuzzle> Fifteens { get; set; } = null!;
        public DbSet<AlphaBeta> AlphaBeta { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<UserGroups> UserGroups { get; set; } = null!;

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

            modelBuilder.Entity<UserGroups>().HasNoKey();
        }
    }
}
