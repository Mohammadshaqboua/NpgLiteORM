using System.Data;
using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Abstract;

public abstract class DbConnectionBase : IDbConnectionFactory
{
    protected readonly string connectionString;

    public DbConnectionBase(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public abstract IDbConnection CreateConnection();
}