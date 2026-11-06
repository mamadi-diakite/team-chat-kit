namespace TeamChat.DemoHost.Api.Persistence;

/// <summary>The demo host's own accounts. The chat tables live in the same database under their own context.</summary>
public sealed class DemoHostDbContext(DbContextOptions<DemoHostDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(builder =>
        {
            builder.ToTable("Organizations");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Name).HasMaxLength(Organization.NameMaxLength).IsRequired();
            builder.Property(o => o.NormalizedName).HasMaxLength(Organization.NameMaxLength).IsRequired();
            builder.HasIndex(o => o.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<UserAccount>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).HasMaxLength(UserAccount.EmailMaxLength).IsRequired();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.DisplayName).HasMaxLength(UserAccount.DisplayNameMaxLength).IsRequired();
            builder.Property(u => u.PasswordHash).HasMaxLength(UserAccount.PasswordHashMaxLength).IsRequired();
            builder.HasOne(u => u.Organization)
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}

/// <summary>SQLite has no time zone support, so dates are stored as UTC and read back as UTC.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
