using System.Data.Common;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Exceptions;
using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private DbConnection _connection;
    private DbTransaction? _transaction;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

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

    public IRepository<T> Repository<T>() where T : EntityBase
    {
        if (_repositories.ContainsKey(typeof(T)))
        {
            return (IRepository<T>)_repositories[typeof(T)];
        }

        var repositoryType = typeof(Repository<>).MakeGenericType(typeof(T));
        var repository = Activator.CreateInstance(repositoryType, _connection) as IRepository<T>;

        _repositories[typeof(T)] = repository!;
        return repository!;
    }

    public async Task Transaction()
    {
        _transaction = await _connection.BeginTransactionAsync();
    }

    public async Task SaveChangesAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction. Call Transaction() before SaveChangesAsync().");
        }

        await _transaction.CommitAsync();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }

        await _transaction.RollbackAsync();
        _transaction = null;
    }
}