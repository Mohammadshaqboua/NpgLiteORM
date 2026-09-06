using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Mapping;

/// <summary>Covers SchemaBuilder's CREATE TABLE generation: table naming, constraints, column renaming, and foreign keys.</summary>
public class SchemaBuilderTests
{
    /// <summary>The generated DDL should target the [Table]-supplied name ("users"), not the C# class name ("User").</summary>
    [Fact]
    public void BuildCreateTableSql_ShouldContainTableName()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("CREATE TABLE IF NOT EXISTS users", sql);
    }

    /// <summary>The [PrimaryKey]-attributed Id column should get a "PRIMARY KEY" constraint in the generated DDL.</summary>
    [Fact]
    public void BuildCreateTableSql_ShouldMarkIdAsPrimaryKey()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("PRIMARY KEY", sql);
    }

    /// <summary>A [Column("full_name")]-attributed property should appear under its SQL name, not its C# property name ("Name").</summary>
    [Fact]
    public void BuildCreateTableSql_ShouldUseColumnAttributeName()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<User>();
        Assert.Contains("full_name", sql);
    }

    /// <summary>Order.UserId's [ForeignKey(typeof(User))] should produce a "REFERENCES users(id)" constraint in Order's generated DDL.</summary>
    [Fact]
    public void BuildCreateTableSql_ShouldGenerateForeignKeyConstraint()
    {
        var schemaBuilder = new SchemaBuilder();
        var sql = schemaBuilder.BuildCreateTableSql<Order>();
        Assert.Contains("REFERENCES users(id)", sql);
    }
}