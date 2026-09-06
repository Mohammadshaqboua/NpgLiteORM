using System.Linq.Expressions;
using NpgLiteORM.Core.Query;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Query;

/// <summary>Covers ExpressionTranslator's core translation cases: single comparisons, compound predicates, closures, and LIKE.</summary>
public class ExpressionTranslatorTests
{
    /// <summary>A single ">" comparison should produce "Id > @p0" with the constant bound as the parameter's value.</summary>
    [Fact]
    public void TranslateExpression_ShouldTranslateGreaterThan()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Id > 5;

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("Id > @p0", sql);
        Assert.Equal(5, parameters["p0"]);
    }

    /// <summary>An "==" comparison on a [Column]-renamed property should use the SQL column name ("full_name"), not the C# property name.</summary>
    [Fact]
    public void TranslateExpression_ShouldTranslateEqual()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Name == "Ahmad";

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("full_name = @p0", sql);
        Assert.Equal("Ahmad", parameters["p0"]);
    }

    /// <summary>A "&amp;&amp;" predicate should produce a parenthesized "AND" fragment with two distinct, suffixed parameter names.</summary>
    [Fact]
    public void TranslateExpression_ShouldSupportAndAlso()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Id > 5 && u.Name == "Ahmad";

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("(Id > @p0 AND full_name = @p0_1)", sql);
        Assert.Equal(5, parameters["p0"]);
        Assert.Equal("Ahmad", parameters["p0_1"]);
    }

    /// <summary>A "||" predicate should produce a parenthesized "OR" fragment, mirroring the AndAlso test above.</summary>
    [Fact]
    public void TranslateExpression_ShouldSupportOrElse()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Id == 1 || u.Id == 2;

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("(Id = @p0 OR Id = @p0_1)", sql);
        Assert.Equal(1, parameters["p0"]);
        Assert.Equal(2, parameters["p0_1"]);
    }

    /// <summary>A comparison against a captured local variable (closure) should evaluate the variable's current value, not treat it as a constant expression.</summary>
    [Fact]
    public void TranslateExpression_ShouldSupportClosureOverLocalVariable()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        var minId = 10;
        Expression<Func<User, bool>> expression = u => u.Id > minId;

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("Id > @p0", sql);
        Assert.Equal(10, parameters["p0"]);
    }

    /// <summary>.Contains() on a string property should translate to a LIKE with %wildcards% on both sides.</summary>
    [Fact]
    public void TranslateExpression_ShouldTranslateContainsToLike()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Name.Contains("hma");

        // Act
        var (sql, parameters) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("full_name LIKE @p0", sql);
        Assert.Equal("%hma%", parameters["p0"]);
    }

    /// <summary>An unsupported ExpressionType (e.g. arithmetic Add) should raise NotSupportedException rather than silently producing wrong SQL.</summary>
    [Fact]
    public void GetSqlOperator_ShouldThrow_WhenOperatorNotSupported()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            translator.GetSqlOperator(ExpressionType.Add));
    }
}
