using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Marks a property as the table's primary key. <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/>
/// appends a "PRIMARY KEY" constraint to the generated column definition when this is present.
/// Applied by default to <see cref="NpgLiteORM.Core.Abstract.EntityBase.Id"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyAttribute : Attribute
{
}