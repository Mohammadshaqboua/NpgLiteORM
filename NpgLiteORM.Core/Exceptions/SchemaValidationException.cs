using System;

namespace NpgLiteORM.Core.Exceptions;

public class SchemaValidationException : Exception
{
    public Type EntityType { get; }
    public string PropertyName { get; }

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