using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Mapping;

public class SchemaBuilderTests
{
    [Fact]
    public void BuildCreateTableSql_ShouldContainTableName()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("CREATE TABLE IF NOT EXISTS users", sql);
    }

    [Fact]
    public void BuildCreateTableSql_ShouldMarkIdAsPrimaryKey()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("PRIMARY KEY", sql);
    }

    [Fact]
    public void BuildCreateTableSql_ShouldUseColumnAttributeName()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("full_name", sql);
    }

    [Fact]
    public void BuildCreateTableSql_ShouldGenerateForeignKeyConstraint()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<Order>();
        Assert.Contains("REFERENCES users(id)", sql);
    }
}
