using System.Data;
using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Abstract;

/// <summary>
/// Base class for connection factories. Holds the connection string and forces
/// concrete subclasses (e.g. <see cref="NpgLiteORM.Core.Data.PostgresConnectionFactory"/>)
/// to implement <see cref="CreateConnection"/> for their specific ADO.NET provider.
/// This is the seam that keeps the rest of the library provider-agnostic: nothing
/// outside this class and its subclasses knows it's talking to Npgsql specifically.
/// </summary>
public abstract class DbConnectionBase : IDbConnectionFactory
{
    /// <summary>The connection string used to construct connections. Stored once here so subclasses don't need to manage it themselves.</summary>
    protected readonly string connectionString;

    /// <summary>
    /// Creates the base with a connection string. Subclasses call this via <c>: base(connectionString)</c>.
    /// </summary>
    /// <param name="connectionString">ADO.NET-style connection string for the target database.</param>
    public DbConnectionBase(string connectionString)
    {
        this.connectionString = connectionString;
    }

    /// <summary>
    /// Creates a new, unopened <see cref="IDbConnection"/> for the target database.
    /// Callers (e.g. Repository&lt;T&gt;, QueryBuilder&lt;T&gt;) are responsible for
    /// opening and disposing the connection.
    /// </summary>
    public abstract IDbConnection CreateConnection();
}