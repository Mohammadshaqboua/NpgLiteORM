using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Enums;
using NpgLiteORM.Core.Interfaces;
using NpgLiteORM.Core.Mapping;

namespace NpgLiteORM.Core.Query;

public class QueryBuilder<T> : IQueryBuilder<T> where T : EntityBase, new()
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly List<Expression<Func<T, bool>>> _whereConditions = new();

    private Expression<Func<T, object>>? _orderByExpression;
    private SortDirection _sortDirection;

    private int? _takeLimit;
    private int? _skipLimit;

    private readonly SchemaBuilder _schemaBuilder;
    private readonly EntityMapper<T> _mapper;
    private readonly ExpressionTranslator<T> _translator;
    private readonly SqlGenerator _sqlGenerator;

    public QueryBuilder(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _schemaBuilder = new SchemaBuilder();
        _mapper = new EntityMapper<T>();
        _translator = new ExpressionTranslator<T>();
        _sqlGenerator = new SqlGenerator();
    }

    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _whereConditions.Add(predicate);
        return this;
    }

    public IQueryBuilder<T> OrderBy(Expression<Func<T, object>> expression)
    {
        _orderByExpression = expression;
        _sortDirection = SortDirection.Ascending;
        return this;
    }

    public IQueryBuilder<T> OrderByDescending(Expression<Func<T, object>> expression)
    {
        _orderByExpression = expression;
        _sortDirection = SortDirection.Descending;
        return this;
    }

    public IQueryBuilder<T> Take(int count)
    {
        _takeLimit = count;
        return this;
    }

    public IQueryBuilder<T> Skip(int count)
    {
        _skipLimit = count;
        return this;
    }

    public async Task<IEnumerable<T>> ExecuteAsync()
    {
        var tableName = _schemaBuilder.GetTableName<T>();

        var whereClauses = new List<string>();
        var parameterValues = new Dictionary<string, object>();

        for (int i = 0; i < _whereConditions.Count; i++)
        {
            var parameterName = $"p{i}";
            var (sql, value) = _translator.TranslateExpression(_whereConditions[i], parameterName);

            whereClauses.Add(sql);
            parameterValues[parameterName] = value;
        }

        string? orderByColumn = null;
        if (_orderByExpression != null)
        {
            var memberExpression = _orderByExpression.Body as MemberExpression;
            if (memberExpression != null)
            {
                var propertyInfo = memberExpression.Member as PropertyInfo;
                orderByColumn = AttributeHelper.GetColumnName(propertyInfo);
            }
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

        return Enumerable.Empty<T>();
    }
}