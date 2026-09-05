using System.Data.Common;

namespace NpgLiteORM.Core.Migrations;

public interface IMigration
{
    int Version { get; }
    string Name { get; }
    Task UpAsync(DbConnection connection);
    Task DownAsync(DbConnection connection);
}