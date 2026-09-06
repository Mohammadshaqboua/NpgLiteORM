using System.Data.Common;
using NpgLiteORM.Core.Migrations;
using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;

namespace NpgLiteORM.Demo.Migrations;

/// <summary>
/// Second migration: creates the "Orders" table, including its foreign key to
/// "users". Runs after <c>CreateUsersTable</c> (Version 2 vs 1) so the referenced
/// table already exists.
/// </summary>
public class CreateOrdersTable : IMigration
{
    /// <inheritdoc />
    public int Version => 2;

    /// <inheritdoc />
    public string Name => "Create Orders Table";

    /// <summary>Generates and executes the CREATE TABLE statement for <see cref="Order"/> via <see cref="SchemaBuilder"/>.</summary>
    public async Task UpAsync(DbConnection connection)
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<Order>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Drops the orders table, undoing <see cref="UpAsync"/>.</summary>
    public async Task DownAsync(DbConnection connection)
    {
        var tableName = new SchemaBuilder().GetTableName<Order>();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
        await command.ExecuteNonQueryAsync();
    }
}