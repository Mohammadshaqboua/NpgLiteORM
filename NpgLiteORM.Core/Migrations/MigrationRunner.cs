using System.Data.Common;

namespace NpgLiteORM.Core.Migrations;

public class MigrationRunner
{
    private readonly DbConnection _connection;

    public MigrationRunner(DbConnection connection)
    {
        _connection = connection;
    }

    public async Task RunAsync(IEnumerable<IMigration> migrations)
    {
        var orderedMigrations = migrations.OrderBy(m => m.Version);

        foreach (var migration in orderedMigrations)
        {
            Console.WriteLine($"Applying migration {migration.Version}: {migration.Name}");
            await migration.UpAsync(_connection);
        }
    }
}