using System;

namespace NpgLiteORM.Core.Exceptions;

/// <summary>
/// Thrown when a lookup or update/delete by ID doesn't match any row — e.g.
/// <see cref="NpgLiteORM.Core.Repositories.Repository{T}.GetByIdAsync"/> finds nothing,
/// or UpdateAsync/DeleteAsync affects zero rows. Carries the entity type and the ID
/// that was searched for, instead of a bare message string.
/// </summary>
public class EntityNotFoundException : Exception
{
    /// <summary>The entity type that was being looked up (e.g. <c>typeof(User)</c>).</summary>
    public Type EntityType { get; }

    /// <summary>The ID value that could not be found.</summary>
    public object EntityId { get; }

    /// <summary>
    /// Creates the exception and builds a readable message from the entity type and ID.
    /// </summary>
    /// <param name="entityType">The entity type that was being looked up.</param>
    /// <param name="entityId">The ID value that could not be found.</param>
    public EntityNotFoundException(Type entityType, object entityId)
        : base($"{entityType.Name} with ID {entityId} was not found.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}