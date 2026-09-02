using System.Threading.Tasks;
using NpgLiteORM.Core.Abstract;

namespace NpgLiteORM.Core.Interfaces;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : EntityBase;
    Task Transaction();
    Task SaveChangesAsync();
}