using System;

namespace NpgLiteORM.Core.Exceptions;

/// <summary>
/// Thrown when a database connection could not be established or used correctly
/// (e.g. the factory returned a connection type the caller couldn't work with).
/// Carries the database name and host so the caller gets structured diagnostic
/// data instead of a bare message string.
/// </summary>
public class ConnectionException : Exception
{
    /// <summary>Name of the database the connection attempt targeted.</summary>
    public string DatabaseName { get; }

    /// <summary>Host/server the connection attempt targeted.</summary>
    public string Host { get; }

    /// <summary>
    /// Creates the exception with diagnostic context plus a human-readable message.
    /// </summary>
    /// <param name="databaseName">Name of the target database.</param>
    /// <param name="host">Host/server that was being connected to.</param>
    /// <param name="message">Explanation of what went wrong.</param>
    public ConnectionException(
        string databaseName,
        string host,
        string message
    ) : base(message)
    {
        DatabaseName = databaseName;
        Host = host;
    }
}