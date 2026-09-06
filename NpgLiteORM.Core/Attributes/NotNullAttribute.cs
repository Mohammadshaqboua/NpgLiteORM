using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Marks a property as required at the database level.
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> appends a "NOT NULL" constraint
/// to the generated column definition when this is present. Note: this is a
/// database-level constraint only — it is not currently validated in C# before
/// an insert/update is attempted.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotNullAttribute : Attribute
{
}