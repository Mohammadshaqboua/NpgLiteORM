using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NpgLiteORM.Core.Interfaces;

public interface IQueryBuilder<T>
{
    IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IQueryBuilder<T> OrderBy(Expression<Func<T, object>> keySelector);
    IQueryBuilder<T> OrderByDescending(Expression<Func<T, object>> keySelector);
    IQueryBuilder<T> Take(int count);
    IQueryBuilder<T> Skip(int count);
    Task<IEnumerable<T>> ExecuteAsync();
}