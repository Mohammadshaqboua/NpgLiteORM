using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Abstract;

/// <summary>
/// Shared base for all repositories. Holds the connection factory every repository
/// needs and provides a minimal, overridable validation hook. Concrete CRUD behavior
/// lives in <see cref="NpgLiteORM.Core.Repositories.Repository{T}"/>, which extends this class.
/// </summary>
/// <typeparam name="T">The entity type this repository works with. Must derive from <see cref="EntityBase"/>.</typeparam>
public abstract class RepositoryBase<T> where T : EntityBase
{
    /// <summary>Factory used to obtain database connections. Protected so derived repositories can use it directly.</summary>
    protected readonly IDbConnectionFactory connectionFactory;

    /// <summary>
    /// Stores the connection factory for use by derived repositories.
    /// </summary>
    /// <param name="factory">Factory the derived repository will use to open connections.</param>
    public RepositoryBase(IDbConnectionFactory factory)
    {
        connectionFactory = factory;
    }

    /// <summary>
    /// Minimal validation hook subclasses can override to add entity-specific checks
    /// before a write. The base implementation only checks the entity isn't null.
    /// </summary>
    /// <param name="entity">The entity instance to validate.</param>
    /// <returns><c>true</c> if the entity is considered valid.</returns>
    public virtual bool Validate(T entity)
    {
        return entity != null;
    }
}