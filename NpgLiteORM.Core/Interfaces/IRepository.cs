using NpgLiteORM.Core.Abstract;

namespace NpgLiteORM.Core.Interfaces;

/// <summary>
/// Standard CRUD contract every entity repository implements. Consumers should code
/// against this interface (not <see cref="NpgLiteORM.Core.Repositories.Repository{T}"/>
/// directly) so the concrete implementation can be swapped or mocked.
/// </summary>
/// <typeparam name="T">The entity type this repository manages. Must derive from <see cref="EntityBase"/>.</typeparam>
public interface IRepository<T> where T : EntityBase
{
    /// <summary>Fetches a single entity by its primary key. Throws if not found.</summary>
    Task<T> GetByIdAsync(int id);

    /// <summary>Fetches every row in the entity's table.</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>Inserts a new row and assigns the generated ID back onto <paramref name="entity"/>.</summary>
    Task AddAsync(T entity);

    /// <summary>Updates the row matching <paramref name="entity"/>'s ID with its current property values.</summary>
    Task UpdateAsync(T entity);

    /// <summary>Deletes the row with the given primary key. Throws if no row was affected.</summary>
    Task DeleteAsync(int id);
}