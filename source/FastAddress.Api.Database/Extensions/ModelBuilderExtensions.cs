using Microsoft.EntityFrameworkCore;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// Extensions for <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    private const string TrigramExtension = "pg_trgm";

    /// <summary>
    /// Enables the <c>pg_trgm</c> PostgreSQL extension.
    /// </summary>
    /// <remarks>
    /// <c>pg_trgm</c> provides trigram-based text similarity. The <c>%</c> and <c>similarity()</c>
    /// operators and the <c>gin_trgm_ops</c> operator class used by the fuzzy text index. It must be
    /// enabled (the migration emits <c>CREATE EXTENSION</c>) before any trigram index or query works.
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ModelBuilder HasTrigramExtension(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        return modelBuilder.HasPostgresExtension(TrigramExtension);
    }
}
