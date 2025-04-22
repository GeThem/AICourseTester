using Microsoft.EntityFrameworkCore;
using AICourseTester.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Reflection.Metadata;
using AICourseTester.Services;
using Laraue.EfCoreTriggers.Common.Extensions;

namespace AICourseTester.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? Group { get; set; }
    }

    public class MainDbContext : IdentityDbContext<ApplicationUser>
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        public DbSet<FifteenPuzzle> Fifteens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.Dimensions)
                .HasDefaultValue(4);

            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.TreeDepth)
                .HasDefaultValue(3);


            modelBuilder.Entity<FifteenPuzzle>()
                .Property(fp => fp.IsSolved)
                .HasDefaultValue(true);
        }
    }
}
