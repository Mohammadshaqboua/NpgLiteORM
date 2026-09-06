using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Enums;
using NpgLiteORM.Core.Interfaces;
using NpgLiteORM.Core.Mapping;

namespace NpgLiteORM.Core.Query;

/// <summary>
/// Fluent, LINQ-style query builder for a single entity type — the piece that lets
/// callers write <c>Where(x => x.Age > 18).OrderByDescending(x => x.CreatedAt).Take(10).ExecuteAsync()</c>
/// instead of hand-writing SQL. Internally it just coordinates three single-purpose
/// helpers: <see cref="ExpressionTranslator{T}"/> (LINQ → WHERE fragment),
/// <see cref="SqlGenerator"/> (fragments → full SQL), and <see cref="EntityMapper{T}"/>
/// (rows → entities).
/// </summary>
/// <typeparam name="T">The entity type being queried. Must derive from <see cref="EntityBase"/>.</typeparam>
public class QueryBuilder<T> : IQueryBuilder<T> where T : EntityBase, new()
{
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>All Where() predicates accumulated so far; combined with AND when the query runs.</summary>
    private readonly List<Expression<Func<T, bool>>> _whereConditions = new();

    private Expression<Func<T, object>>? _orderByExpression;
    private SortDirection _sortDirection;

    private int? _takeLimit;
    private int? _skipLimit;

    private readonly SchemaBuilder _schemaBuilder;
    private readonly EntityMapper<T> _mapper;
    private readonly ExpressionTranslator<T> _translator;
    private readonly SqlGenerator _sqlGenerator;

    /// <summary>
    /// Creates the query builder with the connection factory it will use when <see cref="ExecuteAsync"/> runs.
    /// </summary>
    /// <param name="connectionFactory">Factory used to open a connection for the final query.</param>
    public QueryBuilder(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _schemaBuilder = new SchemaBuilder();
        _mapper = new EntityMapper<T>();
        _translator = new ExpressionTranslator<T>();
        _sqlGenerator = new SqlGenerator();
    }

    /// <summary>
    /// Adds a filter condition. Calling this more than once ANDs every condition together
    /// (in addition to whatever &amp;&amp;/|| logic is already inside a single predicate).
    /// </summary>
    /// <param name="predicate">The condition to filter by, e.g. <c>x => x.Age > 18</c>.</param>
    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _whereConditions.Add(predicate);
        return this;
    }

    /// <summary>Sorts results ascending by the given property. Overwrites any previous OrderBy/OrderByDescending call.</summary>
    /// <param name="expression">Property selector, e.g. <c>x => x.CreatedAt</c>.</param>
    public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> expression)
    {
        _orderByExpression = expression;
        _sortDirection = SortDirection.Ascending;
        return this;
    }

    /// <summary>Sorts results descending by the given property. Overwrites any previous OrderBy/OrderByDescending call.</summary>
    /// <param name="expression">Property selector, e.g. <c>x => x.CreatedAt</c>.</param>
    public IQueryBuilder<T> OrderByDescending(Expression<Func<T, object>> expression)
    {
        _orderByExpression = expression;
        _sortDirection = SortDirection.Descending;
        return this;
    }

    /// <summary>Limits the result set to at most <paramref name="count"/> rows (SQL LIMIT).</summary>
    public IQueryBuilder<T> Take(int count)
    {
        _takeLimit = count;
        return this;
    }

    /// <summary>Skips the first <paramref name="count"/> rows (SQL OFFSET).</summary>
    public IQueryBuilder<T> Skip(int count)
    {
        _skipLimit = count;
        return this;
    }

    /// <summary>
    /// Compiles every Where()/OrderBy()/Take()/Skip() call made so far into one SQL statement,
    /// runs it, and maps the resulting rows back into entities. This is the only method that
    /// actually touches the database — everything before it just builds up state.
    /// </summary>
    /// <returns>The matching rows, mapped to <typeparamref name="T"/> instances.</returns>
    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        var tableName = _schemaBuilder.GetTableName<T>();

        var whereClauses = new List<string>();
        var parameterValues = new Dictionary<string, object?>();

        // Each Where() call gets its own parameter-name prefix ("p0", "p1", ...) so
        // conditions from different calls never collide, even if each one internally
        // expands into several parameters (e.g. a compound && predicate).
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var parameterName = $"p{i}";
            var (sql, values) = _translator.TranslateExpression(_whereConditions[i], parameterName);

            whereClauses.Add(sql);
            foreach (var kvp in values)
            {
                parameterValues[kvp.Key] = kvp.Value;
            }
        }

        string? orderByColumn = null;
        if (_orderByExpression?.Body is MemberExpression { Member: PropertyInfo propertyInfo })
        {
            orderByColumn = AttributeHelper.GetColumnName(propertyInfo);
        }
        else if (_orderByExpression?.Body is UnaryExpression { Operand: MemberExpression { Member: PropertyInfo boxedProperty } })
        {
            // Value-type properties (int, DateTime, ...) get wrapped in a Convert-to-object
            // node when used inside an Expression<Func<T, object>>; unwrap it the same way
            // before reading the property's column name.
            orderByColumn = AttributeHelper.GetColumnName(boxedProperty);
        }

        var finalSql = _sqlGenerator.BuildSelectQuery(
            tableName,
            whereClauses,
            orderByColumn,
            _sortDirection,
            _takeLimit,
            _skipLimit);

        var connection = _connectionFactory.CreateConnection();
        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync();

            using var command = dbConnection.CreateCommand();
            command.CommandText = finalSql;

            foreach (var kvp in parameterValues)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@" + kvp.Key;
                parameter.Value = kvp.Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            using var reader = await command.ExecuteReaderAsync();

            var results = new List<T>();
            while (await reader.ReadAsync())
            {
                results.Add(_mapper.MapToEntity(reader));
            }

            return results;
        }

        // The factory produced a connection type we don't know how to open (not a
        // DbConnection) — rather than throw mid-query, fail soft with an empty result.
        return Enumerable.Empty<T>();
    }
}
