using System.Data;
using Npgsql;
using NpgLiteORM.Core.Abstract;

namespace NpgLiteORM.Core.Data;

public class PostgresConnectionFactory : DbConnectionBase
{
    public PostgresConnectionFactory(string connectionString) : base(connectionString)
    {
    }

    public override IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}