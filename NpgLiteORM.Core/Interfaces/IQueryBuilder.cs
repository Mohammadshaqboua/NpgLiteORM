using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NpgLiteORM.Core.Interfaces;

/// <summary>
/// Fluent, LINQ-style contract for building and running a filtered/sorted/paged
/// SELECT query against a single entity's table. Implemented by
/// <see cref="NpgLiteORM.Core.Query.QueryBuilder{T}"/>. Every method returns
/// <c>this</c> (as the interface) so calls can be chained:
/// <c>Where(...).OrderBy(...).Take(10).ExecuteAsync()</c>.
/// </summary>
public interface IQueryBuilder<T>
{
    /// <summary>Adds a filter condition. Multiple calls are combined with AND.</summary>
    IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);

    /// <summary>Sorts results ascending by the given property.</summary>
    IQueryBuilder<T> OrderBy(Expression<Func<T, object>> keySelector);

    /// <summary>Sorts results descending by the given property.</summary>
    IQueryBuilder<T> OrderByDescending(Expression<Func<T, object>> keySelector);

    /// <summary>Limits the result set to at most <paramref name="count"/> rows (SQL LIMIT).</summary>
    IQueryBuilder<T> Take(int count);

    /// <summary>Skips the first <paramref name="count"/> rows (SQL OFFSET).</summary>
    IQueryBuilder<T> Skip(int count);

    /// <summary>Runs the composed query against the database and maps the rows back to entities.</summary>
    Task<IEnumerable<T>> ExecuteAsync();
}