using NpgLiteORM.Core.Enums;

namespace NpgLiteORM.Core.Query;

/// <summary>
/// Assembles a final SELECT statement from already-translated pieces (WHERE fragments,
/// an ORDER BY column, LIMIT/OFFSET). Deliberately knows nothing about expression trees
/// or reflection — that separation is what keeps <see cref="ExpressionTranslator{T}"/>
/// ("what to filter") and this class ("how to write SQL") independently testable.
/// </summary>
public class SqlGenerator
{
    /// <summary>
    /// Builds a <c>SELECT *</c> statement, appending WHERE / ORDER BY / LIMIT / OFFSET
    /// clauses only for the pieces that were actually supplied.
    /// </summary>
    /// <param name="tableName">The table to select from.</param>
    /// <param name="whereConditions">Zero or more already-translated WHERE fragments (e.g. "age &gt; @p0"); combined with AND.</param>
    /// <param name="orderByColumn">Column to sort by, or null to skip ORDER BY entirely.</param>
    /// <param name="sortDirection">Sort direction to use when <paramref name="orderByColumn"/> is supplied.</param>
    /// <param name="take">Maximum row count (SQL LIMIT), or null to skip it.</param>
    /// <param name="skip">Number of rows to skip (SQL OFFSET), or null to skip it.</param>
    /// <returns>The complete SELECT statement.</returns>
    public string BuildSelectQuery(
        string tableName,
        List<string> whereConditions,
        string? orderByColumn,
        SortDirection sortDirection,
        int? take,
        int? skip)
    {
        var sql = $"SELECT * FROM {tableName}";

        if (whereConditions.Any())
        {
            sql += " WHERE " + string.Join(" AND ", whereConditions);
        }

        if (orderByColumn != null)
        {
            var direction = sortDirection == SortDirection.Descending ? "DESC" : "ASC";
            sql += $" ORDER BY {orderByColumn} {direction}";
        }

        if (take != null)
        {
            sql += $" LIMIT {take}";
        }

        if (skip != null)
        {
            sql += $" OFFSET {skip}";
        }

        return sql;
    }
}
