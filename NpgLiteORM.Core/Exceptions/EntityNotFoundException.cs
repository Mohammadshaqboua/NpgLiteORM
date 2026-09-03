using System;

namespace NpgLiteORM.Core.Exceptions;

public class EntityNotFoundException : Exception
{
    public Type EntityType { get; }
    public object EntityId { get; }

    public EntityNotFoundException(Type entityType, object entityId)
        : base($"{entityType.Name} with ID {entityId} was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}