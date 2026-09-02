using System.Data;

namespace NpgLiteORM.Core.Interfaces;

public interface IDbConnectionFactory
{
     IDbConnection CreateConnection();
}