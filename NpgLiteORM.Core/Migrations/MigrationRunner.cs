using System.Data.Common;

namespace NpgLiteORM.Core.Migrations;

/// <summary>
/// Applies a set of <see cref="IMigration"/> implementations in version order over a
/// single shared connection. Used by NpgLiteORM.Demo/Program.cs to bring a
/// fresh database up to the current schema.
/// </summary>
public class MigrationRunner
{
    /// <summary>The already-open connection every migration's UpAsync will run against.</summary>
    private readonly DbConnection _connection;

    /// <summary>
    /// Creates the runner with the connection to apply migrations on.
    /// </summary>
    /// <param name="connection">An already-open database connection.</param>
    public MigrationRunner(DbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Sorts the given migrations by <see cref="IMigration.Version"/> (ascending) and
    /// applies each one's <see cref="IMigration.UpAsync"/> in turn, logging progress
    /// to the console as it goes.
    /// </summary>
    /// <param name="migrations">The full set of migrations to apply, in any order.</param>
    public async Task RunAsync(IEnumerable<IMigration> migrations)
    {
        // Sort so migrations always apply in a deterministic, dependency-safe order
        // (e.g. "create users" before "create orders" which references users).
        var orderedMigrations = migrations.OrderBy(m => m.Version);

        foreach (var migration in orderedMigrations)
        {
            Console.WriteLine($"Applying migration {migration.Version}: {migration.Name}");
            await migration.UpAsync(_connection);
        }
    }
}