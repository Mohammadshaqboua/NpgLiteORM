using System.Threading.Tasks;
using NpgLiteORM.Core.Abstract;

namespace NpgLiteORM.Core.Interfaces;

/// <summary>
/// Coordinates multiple repositories over a single shared connection/transaction —
/// the same role <c>DbContext.SaveChanges()</c> plays in EF Core. Implemented by
/// <see cref="NpgLiteORM.Core.Repositories.UnitOfWork"/>.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Returns a repository for entity type <typeparamref name="T"/> that shares this
    /// unit of work's connection/transaction. Repositories are created lazily and cached.
    /// </summary>
    IRepository<T> Repository<T>() where T : EntityBase;

    /// <summary>Begins a database transaction on the shared connection.</summary>
    Task Transaction();

    /// <summary>Commits the active transaction started by <see cref="Transaction"/>.</summary>
    Task SaveChangesAsync();
}