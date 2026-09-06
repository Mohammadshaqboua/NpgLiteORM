using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Overrides the SQL column name for a property (e.g. map <c>Name</c> to <c>full_name</c>).
/// Read via <see cref="NpgLiteORM.Core.AttributeHelper.GetColumnName"/>. If omitted, the
/// property's own name is used as the column name.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute : Attribute
{
    /// <summary>The physical column name in the database.</summary>
    public string Name { get; }

    /// <summary>
    /// Creates the attribute with the given column name.
    /// </summary>
    /// <param name="name">Column name to use, e.g. "full_name".</param>
    public ColumnAttribute(string name) => Name = name;
}