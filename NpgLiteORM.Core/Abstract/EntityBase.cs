using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Core.Abstract;

/// <summary>
/// Base class every entity must inherit from. Provides the columns that all tables
/// in this ORM are assumed to have: a primary key (<see cref="Id"/>) and two audit
/// timestamps. Inheriting from this class is what lets generic constraints like
/// <c>where T : EntityBase, new()</c> work across
/// <see cref="NpgLiteORM.Core.Repositories.Repository{T}"/>,
/// <see cref="NpgLiteORM.Core.Query.QueryBuilder{T}"/>, etc.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Primary key column. Marked auto-increment, so <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/>
    /// generates it as SERIAL/BIGSERIAL and the database assigns the value on insert.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Timestamp set when the entity is first created, defaulting to the moment the object is constructed.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp meant to track the last update to the entity. Not currently auto-refreshed by Repository.UpdateAsync.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}