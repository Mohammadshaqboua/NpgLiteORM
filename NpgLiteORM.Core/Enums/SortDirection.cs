namespace NpgLiteORM.Core.Enums;

/// <summary>
/// Direction used by <see cref="NpgLiteORM.Core.Query.QueryBuilder{T}.OrderBy"/> /
/// OrderByDescending and translated into SQL's ASC/DESC by
/// <see cref="NpgLiteORM.Core.Query.SqlGenerator.BuildSelectQuery"/>.
/// </summary>
public enum SortDirection
{
    /// <summary>Smallest-to-largest ordering (SQL "ASC").</summary>
    Ascending,

    /// <summary>Largest-to-smallest ordering (SQL "DESC").</summary>
    Descending
}