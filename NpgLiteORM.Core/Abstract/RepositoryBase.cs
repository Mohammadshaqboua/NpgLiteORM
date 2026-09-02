using NpgLiteORM.Core.Interfaces;

namespace NpgLiteORM.Core.Abstract;

public abstract class RepositoryBase<T> where T : EntityBase
{
    protected readonly IDbConnectionFactory connectionFactory;

    public RepositoryBase(IDbConnectionFactory factory)
    {
        connectionFactory = factory;
    }

    public virtual bool Validate(T entity)
    {
        return entity != null;
    }
}