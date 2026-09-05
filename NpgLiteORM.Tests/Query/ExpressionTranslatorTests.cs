using System.Linq.Expressions;
using NpgLiteORM.Core.Query;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Query;

public class ExpressionTranslatorTests
{
    [Fact]
    public void TranslateExpression_ShouldTranslateGreaterThan()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Id > 5;

        // Act
        var (sql, value) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("Id > @p0", sql);
        Assert.Equal(5, value);
    }

    [Fact]
    public void TranslateExpression_ShouldTranslateEqual()
    {
        // Arrange
        var translator = new ExpressionTranslator<User>();
        Expression<Func<User, bool>> expression = u => u.Name == "Ahmad";

        // Act
        var (sql, value) = translator.TranslateExpression(expression, "p0");

        // Assert
        Assert.Equal("full_name = @p0", sql);
        Assert.Equal("Ahmad", value);
    }

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