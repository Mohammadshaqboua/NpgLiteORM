using System.Data.Common;

namespace NpgLiteORM.Core.Migrations;

/// <summary>
/// Represents one reversible schema change, applied and rolled back in the order
/// given by <see cref="Version"/>. Run through <see cref="MigrationRunner"/>.
/// Implementations typically call <see cref="NpgLiteORM.Core.Mapping.SchemaBuilder"/>
/// to generate the DDL for <see cref="UpAsync"/>.
/// </summary>
public interface IMigration
{
    /// <summary>Ordering key; migrations run from lowest to highest version.</summary>
    int Version { get; }

    /// <summary>Human-readable name shown in <see cref="MigrationRunner"/> console output.</summary>
    string Name { get; }

    /// <summary>Applies this migration's schema change (e.g. CREATE TABLE).</summary>
    Task UpAsync(DbConnection connection);

    /// <summary>Reverts this migration's schema change (e.g. DROP TABLE).</summary>
    Task DownAsync(DbConnection connection);
}