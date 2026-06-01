using FastAddress.Api.Database.Entities;
using FastAddress.Api.Database.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using NodaTime;

namespace FastAddress.Api.Database;

/// <summary>
/// Fast address database context.
/// </summary>
public sealed class FastAddressDbContext : DbContext
{
    public const string SchemaName = "address";

    public DbSet<StreetAddress> StreetAddresses { get; private set; }
    
    public IClock Clock { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FastAddressDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    /// <param name="timeProvider">Abstraction for providing date and time.</param>
    public FastAddressDbContext(DbContextOptions<FastAddressDbContext> options, IClock timeProvider)
        : base(options)
    {
        Clock = timeProvider;
    }

    // Empty constructor needed for tests
    public FastAddressDbContext() { }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.HasTrigramExtension();

        var streetAddress = modelBuilder.Entity<StreetAddress>();
        streetAddress.HasKey(e => e.Id);
        streetAddress.HasDefaultTimestamps();

        streetAddress.Property(e => e.Country).HasMaxLength(100);
        streetAddress.Property(e => e.GooglePlaceId).HasMaxLength(500);
        streetAddress.Property(e => e.Location).HasGeographyColumnType();
        streetAddress.Property(e => e.PostalCode).HasMaxLength(20);
        streetAddress.Property(e => e.PostalTown).HasMaxLength(100);
        streetAddress.Property(e => e.SearchText).HasMaxLength(250);
        streetAddress.Property(e => e.StreetLine).HasMaxLength(250);

        streetAddress.HasIndex(e => e.GooglePlaceId).IsUnique();
        streetAddress.HasIndex(e => e.Location).HasSpatialIndex();
        streetAddress.HasIndex(e => e.SearchText).HasTrigramIndex();

        streetAddress.ToTable("street_addresses");
    }

    /// <inheritdoc/>
    public override int SaveChanges()
    {
        UpdateEntityTimestamps(Clock.GetCurrentInstant());
        return base.SaveChanges();
    }

    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateEntityTimestamps(Clock.GetCurrentInstant());
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateEntityTimestamps(Clock.GetCurrentInstant());
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateEntityTimestamps(Clock.GetCurrentInstant());
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateEntityTimestamps(Instant now)
    {
        IEnumerable<EntityEntry<IEntityTimestamps>> entries = ChangeTracker.Entries().OfType<EntityEntry<IEntityTimestamps>>();

        foreach (var entry in entries)
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.Added = now;
                entry.Entity.Modified = now;
            }
            else if (entry.State is EntityState.Modified)
            {
                entry.Entity.Modified = now;
            }
        }
    }
}
