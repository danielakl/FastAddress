using FastAddress.Api.Database.Entities;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// Extensions for <see cref="EntityTypeBuilder{TEntity}"/>.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures the entity with timestamps that defaults to UTC now on insert.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">TThe model builder.</param>
    public static void HasDefaultTimestamps<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IEntityTimestamps
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Property(e => e.Added).HasDefaultValueNow();
        builder.Property(e => e.Modified).HasDefaultValueNow();
    }

    
}
