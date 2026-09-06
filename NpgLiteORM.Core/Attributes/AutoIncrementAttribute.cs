using System;

namespace NpgLiteORM.Core.Attributes;

/// <summary>
/// Marks an integer property as database-generated on insert.
/// <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> maps such a property to
/// PostgreSQL's SERIAL (for <c>int</c>) or BIGSERIAL (for <c>long</c>) column types
/// instead of a plain INTEGER/BIGINT.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AutoIncrementAttribute : Attribute
{
}