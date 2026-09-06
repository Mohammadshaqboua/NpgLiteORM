using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Marks a property as requiring a unique value across all rows.
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> appends a "UNIQUE" constraint
/// to the generated column definition when this is present.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UniqueAttribute : Attribute
{
}