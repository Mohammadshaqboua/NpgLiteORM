using System.Data;

namespace NpgLiteORM.Core.Interfaces;

/// <summary>
/// Abstraction over "give me a database connection". Everything else in the library
/// depends on this interface rather than a concrete ADO.NET provider, so the
/// persistence provider (Postgres today) could be swapped without touching
/// repositories, the query builder, or the unit of work.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Creates a new, unopened database connection. Callers are responsible for opening and disposing it.</summary>
    IDbConnection CreateConnection();
}