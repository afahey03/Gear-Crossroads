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
        public DbSet<SetupVote> SetupVotes => Set<SetupVote>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Comment> Comments => Set<Comment>();

        public override int SaveChanges()
        {
            ConvertDatesToUtc();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ConvertDatesToUtc();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ConvertDatesToUtc()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                foreach (var property in entry.Properties)
                {
                    if (property.Metadata.ClrType == typeof(DateTime))
                    {
                        var value = (DateTime)property.CurrentValue!;
                        if (value.Kind != DateTimeKind.Utc)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                        }
                    }
                    else if (property.Metadata.ClrType == typeof(DateTime?))
                    {
                        var value = (DateTime?)property.CurrentValue;
                        if (value.HasValue && value.Value.Kind != DateTimeKind.Utc)
                        {
                            property.CurrentValue = DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
                        }
                    }
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure all DateTime properties to be stored as UTC
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }

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
                .HasKey(si => new { si.SetupId, si.ItemId });

            builder.Entity<SetupItem>()
                .HasOne(si => si.Setup)
                .WithMany(s => s.SetupItems)
                .HasForeignKey(si => si.SetupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SetupItem>()
                .HasOne(si => si.Item)
                .WithMany(i => i.SetupItems)
                .HasForeignKey(si => si.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SetupVote>()
                .HasKey(v => new { v.SetupId, v.UserId });

            builder.Entity<SetupVote>()
                .HasOne(v => v.Setup)
                .WithMany(s => s.Votes)
                .HasForeignKey(v => v.SetupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>()
                .HasIndex(r => new { r.UserId, r.FamilyId });
            builder.Entity<RefreshToken>()
                .HasIndex(r => r.TokenHash)
                .IsUnique();
            builder.Entity<RefreshToken>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.Setup)
                .WithMany(s => s.Comments)
                .HasForeignKey(c => c.SetupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
