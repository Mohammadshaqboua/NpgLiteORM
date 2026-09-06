using System.Data;
using Npgsql;
using NpgLiteORM.Core.Abstract;

namespace NpgLiteORM.Core.Data;

/// <summary>
/// The single place in the entire library that knows about Npgsql/PostgreSQL
/// specifically. Every repository, the query builder, and the unit of work all
/// depend on <see cref="NpgLiteORM.Core.Interfaces.IDbConnectionFactory"/> rather
/// than this class directly, so swapping providers means writing one new class here.
/// </summary>
public class PostgresConnectionFactory : DbConnectionBase
{
    /// <summary>
    /// Creates the factory with the Npgsql connection string to use for every
    /// connection it produces.
    /// </summary>
    /// <param name="connectionString">Npgsql-compatible connection string.</param>
    public PostgresConnectionFactory(string connectionString) : base(connectionString)
    {
    }

    /// <summary>
    /// Creates a new, unopened <see cref="NpgsqlConnection"/> using the stored
    /// connection string. The caller is responsible for opening/disposing it.
    /// </summary>
    public override IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}