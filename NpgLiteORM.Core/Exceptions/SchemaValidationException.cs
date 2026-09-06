using System;

namespace NpgLiteORM.Core.Exceptions;

/// <summary>
/// Thrown by <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/> when an entity property's
/// .NET type has no known PostgreSQL column-type mapping. Carries the offending entity
/// type and property name so the failure points directly at the property to fix,
/// instead of a bare <see cref="NotSupportedException"/> message.
/// </summary>
public class SchemaValidationException : Exception
{
    /// <summary>The entity type that failed schema generation.</summary>
    public Type EntityType { get; }

    /// <summary>The specific property whose .NET type could not be mapped to a SQL type.</summary>
    public string PropertyName { get; }

    /// <summary>
    /// Creates the exception with diagnostic context plus a human-readable message.
    /// </summary>
    /// <param name="entityType">The entity type being processed.</param>
    /// <param name="propertyName">The property whose type is unsupported.</param>
    /// <param name="message">Explanation of what went wrong.</param>
    public SchemaValidationException(
        Type entityType,
        string propertyName,
        string message
    ) : base(message)
    {
        EntityType = entityType;
        PropertyName = propertyName;
    }
}