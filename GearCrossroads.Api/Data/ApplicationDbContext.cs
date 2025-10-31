using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GearCrossroads.Api.Models;

namespace GearCrossroads.Api.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts) : base(opts) { }

        public DbSet<Item> Items => Set<Item>();
        public DbSet<Setup> Setups => Set<Setup>();
        public DbSet<SetupItem> SetupItems => Set<SetupItem>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<SetupTag> SetupTags => Set<SetupTag>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SetupTag>()
                .HasKey(st => new { st.SetupId, st.TagId });

            builder.Entity<SetupTag>()
                .HasOne(st => st.Setup)
                .WithMany(s => s.SetupTags)
                .HasForeignKey(st => st.SetupId);

            builder.Entity<SetupTag>()
                .HasOne(st => st.Tag)
                .WithMany(t => t.SetupTags)
                .HasForeignKey(st => st.TagId);

            builder.Entity<SetupItem>()
                .HasOne(si => si.Setup)
                .WithMany(s => s.SetupItems)
                .HasForeignKey(si => si.SetupId);

            builder.Entity<SetupItem>()
                .HasOne(si => si.Item)
                .WithMany(i => i.SetupItems)
                .HasForeignKey(si => si.ItemId);
        }
    }
}
