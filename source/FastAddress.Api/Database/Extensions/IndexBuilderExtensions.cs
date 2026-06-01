using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FastAddress.Api.Database.Extensions;

/// <summary>
/// Extensions for <see cref="IndexBuilder{TEntity}"/>.
/// </summary>
public static class IndexBuilderExtensions
{
    private const string GinMethod = "gin";
    private const string GistMethod = "gist";
    private const string TrigramOperator = "gin_trgm_ops";

    /// <summary>
    /// Configures a GIN trigram index for fuzzy text matching. Apply to a <see cref="string"/> column.
    /// </summary>
    /// <remarks>
    /// A <c>gin</c> (Generalized Inverted Index) combined with the <c>gin_trgm_ops</c> operator
    /// (from the <c>pg_trgm</c> extension) indexes the 3-character substrings of the column. This is
    /// what makes the similarity (<c>%</c>) and <c>similarity()</c> operators index-backed, so a
    /// trigram prefilter stays fast over a large table instead of scanning every row. Migration
    /// generation fails if the indexed column is not text.
    /// </remarks>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IndexBuilder<TEntity> HasTrigramIndex<TEntity>(this IndexBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasMethod(GinMethod).HasOperators(TrigramOperator);
    }

    /// <summary>
    /// Configures a GiST spatial index. Apply to a geometry/geography column.
    /// </summary>
    /// <remarks>
    /// A <c>gist</c> (Generalized Search Tree) index is the standard PostGIS index for spatial columns.
    /// It accelerates distance ordering and nearest-neighbor (<c>&lt;-&gt;</c>) scans. Migration
    /// generation fails if the indexed column is not spatial.
    /// </remarks>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static IndexBuilder<TEntity> HasSpatialIndex<TEntity>(this IndexBuilder<TEntity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasMethod(GistMethod);
    }
}
