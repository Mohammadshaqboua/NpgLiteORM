using System.Data.Common;
using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;

namespace NpgLiteORM.Core.Migrations.Migrations;

public class CreateUsersTable : IMigration
{
    public int Version => 1;
    public string Name => "Create Users Table";

    public async Task UpAsync(DbConnection connection)
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task DownAsync(DbConnection connection)
    {
        var tableName = new SchemaBuilder().GetTableName<User>();

        using var command = connection.CreateCommand();
        command.CommandText = $"DROP TABLE IF EXISTS {tableName}";
        await command.ExecuteNonQueryAsync();
    }
}