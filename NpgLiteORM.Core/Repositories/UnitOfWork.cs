using System.Data.Common;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Exceptions;
using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Repositories;

/// <summary>
/// Coordinates multiple <see cref="Repository{T}"/> instances over one shared connection
/// and transaction — the same role <c>DbContext.SaveChanges()</c> plays in EF Core.
/// Must be initialized via <see cref="InitializeAsync"/> before any other member is used.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>The single connection shared by every repository this unit of work hands out. Null until <see cref="InitializeAsync"/> runs.</summary>
    private DbConnection? _connection;

    private DbTransaction? _transaction;

    /// <summary>Per-entity-type repository cache, so repeated calls to <see cref="Repository{T}"/> for the same type reuse one instance.</summary>
    private readonly Dictionary<Type, object> _repositories = new();

    /// <summary>
    /// Creates the unit of work with the connection factory it will use to open its shared connection.
    /// </summary>
    /// <param name="connectionFactory">Factory used once by <see cref="InitializeAsync"/> to open the shared connection.</param>
    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Opens the single connection every repository and transaction created by this unit of
    /// work will share. Must be called exactly once, before <see cref="Repository{T}"/> or
    /// <see cref="Transaction"/>.
    /// </summary>
    /// <exception cref="ConnectionException">Thrown if the factory produced a connection that isn't a <see cref="DbConnection"/>.</exception>
    public async Task InitializeAsync()
    {
        var connection = _connectionFactory.CreateConnection();
        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
            _connection = dbConnection;
        }
        else
        {
            throw new ConnectionException("Unknown", "Unknown", "Failed to create a valid database connection.");
        }
    }

    /// <summary>
    /// Returns a repository for entity type <typeparamref name="T"/> that shares this unit
    /// of work's connection (and therefore its active transaction, if any). Repositories are
    /// created lazily on first request and cached for subsequent calls with the same type.
    /// </summary>
    /// <typeparam name="T">The entity type to get a repository for.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if called before <see cref="InitializeAsync"/>.</exception>
    public IRepository<T> Repository<T>() where T : EntityBase
    {
        EnsureInitialized();

        if (_repositories.ContainsKey(typeof(T)))
        {
            return (IRepository<T>)_repositories[typeof(T)];
        }

        // Repository<T> only exists as a closed generic type once T is known at runtime,
        // so it's constructed via reflection rather than `new Repository<T>(...)` directly.
        var repositoryType = typeof(Repository<>).MakeGenericType(typeof(T));
        var repository = Activator.CreateInstance(repositoryType, _connection) as IRepository<T>;

        _repositories[typeof(T)] = repository!;
        return repository!;
    }

    /// <summary>
    /// Begins a database transaction on the shared connection. Every repository obtained
    /// from this unit of work afterwards will participate in it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if called before <see cref="InitializeAsync"/>.</exception>
    public async Task Transaction()
    {
        EnsureInitialized();
        _transaction = await _connection!.BeginTransactionAsync();
    }

    /// <summary>
    /// Commits the transaction started by <see cref="Transaction"/>, persisting every change
    /// made through repositories obtained from this unit of work since then.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is currently active.</exception>
    public async Task SaveChangesAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction. Call Transaction() before SaveChangesAsync().");
        }

        await _transaction.CommitAsync();
        _transaction = null;
    }

    /// <summary>
    /// Rolls back the transaction started by <see cref="Transaction"/>, discarding every
    /// change made through repositories obtained from this unit of work since then.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no transaction is currently active.</exception>
    public async Task RollbackAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }

        await _transaction.RollbackAsync();
        _transaction = null;
    }

    /// <summary>Guards every member that needs the shared connection against being called before <see cref="InitializeAsync"/>.</summary>
    private void EnsureInitialized()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException(
                "UnitOfWork has not been initialized. Call InitializeAsync() before using it.");
        }
    }
}
