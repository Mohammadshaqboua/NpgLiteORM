using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Demo.Models;

[Table("users")]
public class User : EntityBase
{
    [Column("full_name"), NotNull, MaxLength(50)]
    public string Name { get; set; }

    [Column("email"),MaxLength(200), Unique]
    public string Email { get; set; }
}