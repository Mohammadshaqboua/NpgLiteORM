using NpgLiteORM.Core.Enums;

namespace NpgLiteORM.Core.Query;

public class SqlGenerator
{
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