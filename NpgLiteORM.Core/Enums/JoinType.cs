namespace NpgLiteORM.Core.Enums;

/// <summary>
/// SQL join kinds this ORM's metadata model is aware of. Defined ahead of time as
/// groundwork for multi-table queries, but not yet consumed by
/// <see cref="NpgLiteORM.Core.Query.QueryBuilder{T}"/> or
/// <see cref="NpgLiteORM.Core.Query.SqlGenerator"/> (see README "Known Limitations").
/// </summary>
public enum JoinType
{
    /// <summary>SQL INNER JOIN — only matching rows from both tables.</summary>
    Inner,

    /// <summary>SQL LEFT JOIN — all rows from the left table, matched rows from the right.</summary>
    Left,

    /// <summary>SQL RIGHT JOIN — all rows from the right table, matched rows from the left.</summary>
    Right,

    /// <summary>SQL FULL JOIN — all rows from both tables, matched where possible.</summary>
    Full
}