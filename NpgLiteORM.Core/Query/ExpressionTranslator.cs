using System.Linq.Expressions;
using System.Reflection;

namespace NpgLiteORM.Core.Query;

public class ExpressionTranslator<T>
{
    public string GetSqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.LessThan => "<",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException($"Operator {nodeType} not supported")
        };
    }

    public (string sql, object value) TranslateExpression(
        Expression<Func<T, bool>> expression,
        string parameterName)
    {
        var body = expression.Body as BinaryExpression;
        var left = body.Left as MemberExpression;
        var propertyInfo = left.Member as PropertyInfo;
        var columnName = AttributeHelper.GetColumnName(propertyInfo);

        var right = body.Right as ConstantExpression;
        var value = right.Value;

        var sqlOperator = GetSqlOperator(body.NodeType);

        return ($"{columnName} {sqlOperator} @{parameterName}", value);
    }
}