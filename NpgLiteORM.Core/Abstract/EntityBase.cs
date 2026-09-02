using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Core.Abstract;

public abstract class EntityBase
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}