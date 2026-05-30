using FastAddress.Api.Database.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NodaTime;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// Extensions for <see cref="EntityTypeBuilder{TEntity}"/>.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    // Returns a "timestamp with time zone". now() -> 2019-12-23 14:39:53.662522-05
    private const string PostgreSqlNow = "now()";

    /// <summary>
    /// Configures the entity with timestamps that defaults to UTC now on insert.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">TThe model builder.</param>
    public static void HasDefaultTimestamps<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntityTimestamps
    {
        builder.Property(e => e.Added).HasDefaultValueNow();
        builder.Property(e => e.Modified).HasDefaultValueNow();
    }

    /// <summary>
    /// Configures the timestamp property to default to now() on insert if the value is its BCL default.
    /// </summary>
    /// <param name="builder">The property builder.</param>
    public static PropertyBuilder<Instant> HasDefaultValueNow(this PropertyBuilder<Instant> builder)
    {
        return builder.HasDefaultValueSql(PostgreSqlNow);
    }
}
