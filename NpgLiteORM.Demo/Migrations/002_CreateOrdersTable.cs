using System.Data.Common;
using NpgLiteORM.Core.Migrations;
using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;

namespace NpgLiteORM.Demo.Migrations;

public class CreateOrdersTable : IMigration
{
    public int Version => 2;
    public string Name => "Create Orders Table";

    public async Task UpAsync(DbConnection connection)
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<Order>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task DownAsync(DbConnection connection)
    {
        var tableName = new SchemaBuilder().GetTableName<Order>();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
        await command.ExecuteNonQueryAsync();
    }
}