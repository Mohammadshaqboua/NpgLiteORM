using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Marks a class as mapped to a PostgreSQL table and supplies the table name.
/// Read by <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder.GetTableName{T}"/> and
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder.BuildCreateTableSql{T}"/>.
/// If omitted, the class's own name is used as the table name instead.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    /// <summary>The physical table name in the database.</summary>
    public string Name { get; }

    /// <summary>
    /// Creates the attribute with the given table name.
    /// </summary>
    /// <param name="name">Table name to use, e.g. "users".</param>
    public TableAttribute(string name) => Name = name;
}