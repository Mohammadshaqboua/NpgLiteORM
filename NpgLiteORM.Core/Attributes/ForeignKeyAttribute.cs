using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Declares a property as a foreign key referencing another mapped entity's table.
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> appends a "REFERENCES table(column)"
/// constraint to the generated column definition when this is present.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ForeignKeyAttribute : Attribute
{
    /// <summary>The entity type whose table this property references (e.g. <c>typeof(User)</c>).</summary>
    public Type ReferencedType { get; }

    /// <summary>The column on the referenced table this property points to. Defaults to "id".</summary>
    public string ReferencedColumn { get; }

    /// <summary>
    /// Creates the attribute.
    /// </summary>
    /// <param name="referencedType">Entity type of the parent table.</param>
    /// <param name="referencedColumn">Column on the parent table being referenced (defaults to "id").</param>
    public ForeignKeyAttribute(Type referencedType, string referencedColumn = "id")
    {
        ReferencedType = referencedType;
        ReferencedColumn = referencedColumn;
    }
}