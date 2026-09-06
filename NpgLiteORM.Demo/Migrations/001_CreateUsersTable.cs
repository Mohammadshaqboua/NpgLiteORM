using System.Data.Common;
using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;

namespace NpgLiteORM.Core.Migrations.Migrations;

/// <summary>
/// First migration: creates the "users" table. Runs before <c>CreateOrdersTable</c>
/// (Version 1 vs 2) since orders reference users via a foreign key.
/// </summary>
public class CreateUsersTable : IMigration
{
    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public string Name => "Create Users Table";

    /// <summary>Generates and executes the CREATE TABLE statement for <see cref="User"/> via <see cref="SchemaBuilder"/>.</summary>
    public async Task UpAsync(DbConnection connection)
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Drops the users table, undoing <see cref="UpAsync"/>.</summary>
    public async Task DownAsync(DbConnection connection)
    {
        var tableName = new SchemaBuilder().GetTableName<User>();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
        await command.ExecuteNonQueryAsync();
    }
}