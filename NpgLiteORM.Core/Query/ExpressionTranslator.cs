using System.Linq.Expressions;
using System.Reflection;

namespace NpgLiteORM.Core.Query;

/// <summary>
/// Translates a strongly-typed LINQ predicate (<c>x => x.Age > 18 &amp;&amp; x.Name == "Ahmad"</c>)
/// into a parameterized SQL WHERE fragment.
///
/// Supports:
///  - Simple comparisons: ==, !=, &gt;, &lt;, &gt;=, &lt;=
///  - Compound predicates: &amp;&amp; (AND), || (OR), and unary ! (NOT)
///  - Captured local variables / closures on the right-hand side (e.g. <c>x => x.Age > minAge</c>)
///  - <c>string.Contains</c> / <c>StartsWith</c> / <c>EndsWith</c>, translated to SQL LIKE
///
/// Every value is bound as a parameter (never string-concatenated), so the generated SQL
/// is safe against SQL injection by construction.
/// </summary>
public class ExpressionTranslator<T>
{
    /// <summary>
    /// Maps a .NET comparison <see cref="ExpressionType"/> (Equal, GreaterThan, ...) to its
    /// SQL operator text. Only comparison operators are supported here — AndAlso/OrElse are
    /// handled one level up in <see cref="TranslateNode"/>, not through this method.
    /// </summary>
    /// <param name="nodeType">The comparison node type from the expression tree.</param>
    /// <returns>The matching SQL operator, e.g. "=" for <see cref="ExpressionType.Equal"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown for any node type that isn't a supported comparison.</exception>
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

    /// <summary>
    /// Entry point: translates the full predicate body into a SQL fragment plus the parameter
    /// values it needs. <paramref name="parameterName"/> is used as-is when the predicate has a
    /// single comparison (keeping simple queries readable, e.g. "age > @p0"); compound predicates
    /// get suffixed parameter names ("@p0", "@p0_1", "@p0_2", ...) so every value has a unique placeholder.
    /// </summary>
    /// <param name="expression">The predicate to translate, e.g. <c>x => x.Age > 18</c>.</param>
    /// <param name="parameterName">Base SQL parameter name to use for the first (or only) value.</param>
    /// <returns>The WHERE fragment and a dictionary of parameter name → value to bind on the command.</returns>
    public (string Sql, Dictionary<string, object?> Parameters) TranslateExpression(
        Expression<Func<T, bool>> expression,
        string parameterName)
    {
        var parameters = new Dictionary<string, object?>();
        var leafIndex = 0;
        var sql = TranslateNode(expression.Body, parameterName, parameters, ref leafIndex);
        return (sql, parameters);
    }

    /// <summary>
    /// Recursively walks one node of the expression tree, dispatching to the right handler
    /// based on what kind of node it is: a logical AND/OR (recurse into both sides), a leaf
    /// comparison, a method call (Contains/StartsWith/EndsWith), or a NOT.
    /// </summary>
    /// <param name="node">The current expression node being translated.</param>
    /// <param name="baseParamName">The parameter-name prefix shared by every leaf in this predicate.</param>
    /// <param name="parameters">Accumulator that leaf translations add their bound values into.</param>
    /// <param name="leafIndex">Running count of leaves seen so far, used to generate unique parameter names.</param>
    /// <returns>The SQL fragment for this node (and everything beneath it).</returns>
    private string TranslateNode(Expression node, string baseParamName, Dictionary<string, object?> parameters, ref int leafIndex)
    {
        switch (node)
        {
            case BinaryExpression { NodeType: ExpressionType.AndAlso or ExpressionType.OrElse } logical:
            {
                // Compound predicate: translate each side independently (they may each
                // contain further nested AND/OR trees) and join with the matching SQL keyword.
                var left = TranslateNode(logical.Left, baseParamName, parameters, ref leafIndex);
                var right = TranslateNode(logical.Right, baseParamName, parameters, ref leafIndex);
                var op = logical.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({left} {op} {right})";
            }

            case BinaryExpression comparison:
                // A leaf comparison like x.Age > 18 — consumes exactly one parameter slot.
                return TranslateComparison(comparison, NextParamName(baseParamName, ref leafIndex), parameters);

            case MethodCallExpression methodCall:
                // x.Name.Contains("a") and friends — also a leaf, consumes one parameter slot.
                return TranslateMethodCall(methodCall, NextParamName(baseParamName, ref leafIndex), parameters);

            case UnaryExpression { NodeType: ExpressionType.Not } not:
                // !(...) — recurse into the negated sub-expression and wrap the result.
                return $"NOT ({TranslateNode(not.Operand, baseParamName, parameters, ref leafIndex)})";

            default:
                throw new NotSupportedException($"Expression of type '{node.NodeType}' is not supported.");
        }
    }

    /// <summary>
    /// Generates the next unique parameter name for a leaf: the very first leaf keeps the
    /// caller-supplied base name unchanged (so simple, single-condition queries stay readable
    /// as "@p0"), and every leaf after that gets a numbered suffix ("@p0_1", "@p0_2", ...).
    /// </summary>
    private static string NextParamName(string baseParamName, ref int leafIndex)
    {
        var name = leafIndex == 0 ? baseParamName : $"{baseParamName}_{leafIndex}";
        leafIndex++;
        return name;
    }

    /// <summary>
    /// Translates a single comparison (==, !=, &gt;, &lt;, &gt;=, &lt;=) into "column operator @param"
    /// and records the bound value.
    /// </summary>
    private string TranslateComparison(BinaryExpression comparison, string paramName, Dictionary<string, object?> parameters)
    {
        var (columnName, valueExpression, flipped) = ResolveOperands(comparison.Left, comparison.Right);
        var value = EvaluateExpression(valueExpression);
        parameters[paramName] = value;

        var sqlOperator = GetSqlOperator(comparison.NodeType);
        if (flipped)
        {
            // The property was on the right (e.g. `18 < x.Age`); reading it left-to-right as
            // "column operator value" means the operator itself needs to flip direction.
            sqlOperator = FlipOperator(sqlOperator);
        }

        return $"{columnName} {sqlOperator} @{paramName}";
    }

    /// <summary>
    /// Translates a supported string method call (<c>Contains</c>, <c>StartsWith</c>,
    /// <c>EndsWith</c>) called on an entity property into a SQL <c>LIKE</c> fragment with
    /// the appropriate <c>%</c> wildcard placement.
    /// </summary>
    private string TranslateMethodCall(MethodCallExpression methodCall, string paramName, Dictionary<string, object?> parameters)
    {
        if (methodCall.Object == null || !TryGetColumnName(methodCall.Object, out var columnName))
        {
            throw new NotSupportedException(
                $"Method '{methodCall.Method.Name}' must be called directly on an entity property (e.g. x.Name.Contains(\"a\")).");
        }

        var argumentValue = EvaluateExpression(methodCall.Arguments[0]);
        var pattern = methodCall.Method.Name switch
        {
            "Contains" => $"%{argumentValue}%",
            "StartsWith" => $"{argumentValue}%",
            "EndsWith" => $"%{argumentValue}",
            _ => throw new NotSupportedException($"Method '{methodCall.Method.Name}' is not supported.")
        };

        parameters[paramName] = pattern;
        return $"{columnName} LIKE @{paramName}";
    }

    /// <summary>
    /// Figures out which side of a comparison is the entity property (the column) and which
    /// side is the value to bind. Supports the natural order (`x.Age > 18`) as well as the
    /// reversed order (`18 &lt; x.Age`) by checking both sides.
    /// </summary>
    /// <returns>The resolved column name, the expression to evaluate for the value, and whether the sides were reversed.</returns>
    /// <exception cref="NotSupportedException">Thrown when neither side is a direct entity property access.</exception>
    private (string ColumnName, Expression ValueExpression, bool Flipped) ResolveOperands(Expression left, Expression right)
    {
        if (TryGetColumnName(left, out var leftColumn))
        {
            return (leftColumn!, right, false);
        }

        if (TryGetColumnName(right, out var rightColumn))
        {
            return (rightColumn!, left, true);
        }

        throw new NotSupportedException("A comparison must reference exactly one entity property.");
    }

    /// <summary>
    /// Checks whether an expression is (after unwrapping any boxing conversion) a direct
    /// property access on the lambda parameter — i.e. it represents an entity column rather
    /// than a constant, a local variable, or some other expression.
    /// </summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="columnName">The resolved SQL column name, if this was a property access.</param>
    /// <returns><c>true</c> and sets <paramref name="columnName"/> if this is a column reference; otherwise <c>false</c>.</returns>
    private static bool TryGetColumnName(Expression expression, out string? columnName)
    {
        var unwrapped = Unwrap(expression);
        if (unwrapped is MemberExpression { Member: PropertyInfo property } memberExpression
            && memberExpression.Expression?.NodeType == ExpressionType.Parameter)
        {
            columnName = AttributeHelper.GetColumnName(property);
            return true;
        }

        columnName = null;
        return false;
    }

    /// <summary>
    /// Strips away compiler-inserted <c>Convert</c>/<c>ConvertChecked</c> boxing nodes
    /// (e.g. from comparing a value-type property inside an <c>object</c>-typed expression)
    /// to get at the real underlying expression.
    /// </summary>
    private static Expression Unwrap(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }

    /// <summary>Reverses a directional comparison operator (used when the property was on the right-hand side).</summary>
    private static string FlipOperator(string sqlOperator) => sqlOperator switch
    {
        ">" => "<",
        "<" => ">",
        ">=" => "<=",
        "<=" => ">=",
        _ => sqlOperator // "=" and "<>" read the same in either direction
    };

    /// <summary>
    /// Evaluates the value side of a comparison to an actual .NET value. Handles plain
    /// constants (`x.Age > 18`, a <see cref="ConstantExpression"/>) as well as captured local
    /// variables / closures (`x.Age > minAge`) by compiling the sub-expression into a delegate
    /// and invoking it, rather than assuming it's always a constant.
    /// </summary>
    private static object? EvaluateExpression(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        var lambda = Expression.Lambda(Expression.Convert(expression, typeof(object)));
        var compiled = (Func<object?>)lambda.Compile();
        return compiled();
    }
}
