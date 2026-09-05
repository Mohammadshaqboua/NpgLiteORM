using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Demo.Models;

[Table("Orders")]
public class Order : EntityBase
{   
    [ForeignKey(typeof(User))]
    public int UserId { get; set; }
    
    [NotNull]
    public decimal Total { get; set; }
}