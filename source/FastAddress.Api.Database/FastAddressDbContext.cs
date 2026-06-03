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

    public DbSet<StreetAddressResult> StreetAddressResults { get; private set; }

    public DbSet<StreetAddressQuery> StreetAddressQueries { get; private set; }

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

        var streetAddressResult = modelBuilder.Entity<StreetAddressResult>();
        streetAddressResult.HasKey(e => e.Id);
        streetAddressResult.HasDefaultTimestamps();

        streetAddressResult.Property(e => e.Country).HasMaxLength(100);
        streetAddressResult.Property(e => e.GooglePlaceId).HasMaxLength(500);
        streetAddressResult.Property(e => e.LastRefreshed).HasDefaultValueNow();
        streetAddressResult.Property(e => e.Location).HasGeographyColumnType();
        streetAddressResult.Property(e => e.PostalCode).HasMaxLength(20);
        streetAddressResult.Property(e => e.PostalTown).HasMaxLength(100);
        streetAddressResult.Property(e => e.SearchText).HasMaxLength(250);
        streetAddressResult.Property(e => e.StreetLine).HasMaxLength(250);

        streetAddressResult.HasIndex(e => e.GooglePlaceId).IsUnique();
        streetAddressResult.HasIndex(e => e.LastRefreshed);
        streetAddressResult.HasIndex(e => e.Location).HasSpatialIndex();
        streetAddressResult.HasIndex(e => e.SearchText).HasTrigramIndex();

        streetAddressResult.ToTable("street_address_results");

        var streetAddressQuery = modelBuilder.Entity<StreetAddressQuery>();
        streetAddressQuery.HasKey(e => e.Id);
        streetAddressQuery.HasDefaultTimestamps();

        streetAddressQuery.Property(e => e.LastRefreshed).HasDefaultValueNow();
        streetAddressQuery.Property(e => e.Query).HasMaxLength(250);

        streetAddressQuery.HasIndex(e => e.Query).IsUnique();

        streetAddressQuery.ToTable("street_address_queries");
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
