using System.Data.Common;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Exceptions;
using NpgLiteORM.Core.Interfaces;
using NpgLiteORM.Core.Mapping;

namespace NpgLiteORM.Core.Repositories;

/// <summary>
/// Default CRUD implementation for a single entity type. Every SQL statement here is
/// built with parameterized <see cref="DbParameter"/> bindings — never string-concatenated
/// user input — which is what makes the library SQL-injection-safe by construction.
///
/// Supports two connection modes: a standalone repository that opens its own connection
/// per call (used directly by consumers), and a "shared connection" mode used internally
/// by <see cref="UnitOfWork"/> so multiple repositories can participate in one transaction.
/// </summary>
/// <typeparam name="T">The entity type this repository manages. Must derive from <see cref="EntityBase"/>.</typeparam>
public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : EntityBase, new()
{
    private SchemaBuilder SchemaBuilder { get; }
    private EntityMapper<T> Mapper { get; }

    /// <summary>Connection supplied by a <see cref="UnitOfWork"/>, when this repository is participating in a shared transaction.</summary>
    private readonly DbConnection? _sharedConnection;

    /// <summary>
    /// Standalone mode: this repository will open and close its own connection
    /// (via <paramref name="factory"/>) for every operation.
    /// </summary>
    /// <param name="factory">Factory used to create a fresh connection per call.</param>
    public Repository(IDbConnectionFactory factory) : base(factory)
    {
        SchemaBuilder = new SchemaBuilder();
        Mapper = new EntityMapper<T>();
    }

    /// <summary>
    /// Shared-connection mode: this repository reuses an already-open connection
    /// (typically one owned by a <see cref="UnitOfWork"/>) instead of opening its own.
    /// </summary>
    /// <param name="sharedConnection">An already-open connection to reuse.</param>
    public Repository(DbConnection sharedConnection) : base(null!)
    {
        _sharedConnection = sharedConnection;
        SchemaBuilder = new SchemaBuilder();
        Mapper = new EntityMapper<T>();
    }

    /// <summary>
    /// Returns the connection this repository should use for its next operation: the
    /// shared connection if one was supplied, otherwise a freshly opened one from the
    /// connection factory.
    /// </summary>
    /// <returns>An open <see cref="DbConnection"/> ready for a command to run against.</returns>
    /// <exception cref="ConnectionException">Thrown if the factory produced a connection that isn't a <see cref="DbConnection"/>.</exception>
    private async Task<DbConnection> GetOpenConnectionAsync()
    {
        if (_sharedConnection != null)
        {
            return _sharedConnection;
        }

        var connection = connectionFactory.CreateConnection();
        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();
            return dbConnection;
        }

        throw new ConnectionException("Unknown", "Unknown", "Failed to create a valid database connection.");
    }

    /// <summary>
    /// Inserts a new row for <paramref name="entity"/> and writes the database-generated
    /// primary key back onto <paramref name="entity"/>.Id.
    /// </summary>
    /// <param name="entity">The entity to insert. Its Id is ignored on the way in and overwritten on the way out.</param>
    public async Task AddAsync(T entity)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var row = Mapper.MapToRow(entity);
        var tableName = SchemaBuilder.GetTableName<T>();

        // The primary key is database-generated (SERIAL/BIGSERIAL), so it must never be
        // part of the INSERT column list — otherwise we'd be inserting Id = 0 explicitly.
        var insertableColumns = row
            .Where(kvp => !kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var columns = string.Join(", ", insertableColumns.Select(kvp => kvp.Key));
        var parameters = string.Join(", ", insertableColumns.Select(kvp => "@" + kvp.Key));
        // RETURNING id lets us grab the generated key in the same round-trip as the insert,
        // instead of a separate SELECT currval()/lastval() call.
        var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}) RETURNING id";

        using var command = dbConnection.CreateCommand();
        command.CommandText = sql;

        foreach (var kvp in insertableColumns)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@" + kvp.Key;
            parameter.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        var newId = await command.ExecuteScalarAsync();
        entity.Id = Convert.ToInt32(newId);
    }

    /// <summary>Fetches every row in the entity's table and maps each one back into a <typeparamref name="T"/>.</summary>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var dbConnection = await GetOpenConnectionAsync();

        var tableName = SchemaBuilder.GetTableName<T>();
        var sql = $"SELECT * FROM {tableName}";

        using var command = dbConnection.CreateCommand();
        command.CommandText = sql;
        using var reader = await command.ExecuteReaderAsync();

        var results = new List<T>();

        while (await reader.ReadAsync())
        {
            var entity = Mapper.MapToEntity(reader);
            results.Add(entity);
        }

        return results;
    }

    /// <summary>Fetches a single entity by its primary key.</summary>
    /// <param name="id">Primary key value to look up.</param>
    /// <exception cref="EntityNotFoundException">Thrown if no row matches <paramref name="id"/>.</exception>
    public async Task<T> GetByIdAsync(int id)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var tableName = SchemaBuilder.GetTableName<T>();
        var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";

        using var command = dbConnection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@Id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return Mapper.MapToEntity(reader);
        }

        throw new EntityNotFoundException(typeof(T), id);
    }

    /// <summary>
    /// Updates every mapped column (except Id) of the row matching <paramref name="entity"/>.Id
    /// with <paramref name="entity"/>'s current property values.
    /// </summary>
    /// <param name="entity">The entity with updated values; its Id identifies which row to update.</param>
    /// <exception cref="EntityNotFoundException">Thrown if no row matched the entity's Id (zero rows affected).</exception>
    public async Task UpdateAsync(T entity)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var tableName = SchemaBuilder.GetTableName<T>();
        var row = Mapper.MapToRow(entity);

        // Id identifies the row (used in the WHERE clause below), so it's excluded from
        // the SET list — you can't update a row's own primary key this way.
        var updatableColumns = row
            .Where(kvp => !kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var setClauses = string.Join(", ", updatableColumns.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
        var sql = $"UPDATE {tableName} SET {setClauses} WHERE id = @id";

        using var command = dbConnection.CreateCommand();
        command.CommandText = sql;

        foreach (var kvp in updatableColumns)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@" + kvp.Key;
            parameter.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        var idParameter = command.CreateParameter();
        idParameter.ParameterName = "@id";
        idParameter.Value = entity.Id;
        command.Parameters.Add(idParameter);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            // Zero rows affected means the Id didn't exist — report it the same way
            // GetByIdAsync does, rather than silently succeeding on a no-op update.
            throw new EntityNotFoundException(typeof(T), entity.Id);
        }
    }

    /// <summary>Deletes the row with the given primary key.</summary>
    /// <param name="id">Primary key of the row to delete.</param>
    /// <exception cref="EntityNotFoundException">Thrown if no row matched <paramref name="id"/> (zero rows affected).</exception>
    public async Task DeleteAsync(int id)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var tableName = SchemaBuilder.GetTableName<T>();
        var sql = $"DELETE FROM {tableName} WHERE id = @id";

        using var command = dbConnection.CreateCommand();
        command.CommandText = sql;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new EntityNotFoundException(typeof(T), id);
        }
    }
}
