// Demo entry point: opens a connection to the local PostgreSQL instance (see
// docker-compose.yml) and runs every migration in order, bringing a fresh
// database up to the current schema (users, then orders).
using System.Data.Common;
using NpgLiteORM.Core.Data;
using NpgLiteORM.Core.Migrations;
using NpgLiteORM.Core.Migrations.Migrations;
using NpgLiteORM.Demo.Migrations;

string connectionString = "Host=localhost;Port=5433;Database=npglite_db;Username=postgres;Password=postgres123";
var connectionFactory = new PostgresConnectionFactory(connectionString);

var connection = connectionFactory.CreateConnection();
if (connection is DbConnection dbConnection)
{
    await dbConnection.OpenAsync();

    var migrationRunner = new MigrationRunner(dbConnection);
    var migrations = new List<IMigration>
    {
        new CreateUsersTable(),
        new CreateOrdersTable()
    };

    await migrationRunner.RunAsync(migrations);
}