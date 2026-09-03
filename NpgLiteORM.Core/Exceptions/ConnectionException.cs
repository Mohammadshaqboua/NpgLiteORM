using System;

namespace NpgLiteORM.Core.Exceptions;

public class ConnectionException : Exception
{
    public string DatabaseName { get; }
    public string Host { get; }

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