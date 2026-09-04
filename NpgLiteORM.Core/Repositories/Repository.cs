using System.Data.Common;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Exceptions;
using NpgLiteORM.Core.Interfaces;
using NpgLiteORM.Core.Mapping;

namespace NpgLiteORM.Core.Repositories;

public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : EntityBase, new()
{
    private SchemaBuilder SchemaBuilder { get; }
    private EntityMapper<T> Mapper { get; }
    private readonly DbConnection? _sharedConnection;

    public Repository(IDbConnectionFactory factory) : base(factory)
    {
        SchemaBuilder = new SchemaBuilder();
        Mapper = new EntityMapper<T>();
    }

    public Repository(DbConnection sharedConnection) : base(null!)
    {
        _sharedConnection = sharedConnection;
        SchemaBuilder = new SchemaBuilder();
        Mapper = new EntityMapper<T>();
    }

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

    public async Task AddAsync(T entity)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var row = Mapper.MapToRow(entity);
        var tableName = SchemaBuilder.GetTableName<T>();

        var insertableColumns = row
            .Where(kvp => !kvp.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var columns = string.Join(", ", insertableColumns.Select(kvp => kvp.Key));
        var parameters = string.Join(", ", insertableColumns.Select(kvp => "@" + kvp.Key));
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

    public async Task UpdateAsync(T entity)
    {
        var dbConnection = await GetOpenConnectionAsync();

        var tableName = SchemaBuilder.GetTableName<T>();
        var row = Mapper.MapToRow(entity);

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
            throw new EntityNotFoundException(typeof(T), entity.Id);
        }
    }

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