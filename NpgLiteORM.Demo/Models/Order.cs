using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Demo.Models;

/// <summary>
/// Sample entity demonstrating a foreign-key relationship: each Order belongs to
/// exactly one <see cref="User"/> via <see cref="UserId"/>. Maps to the "Orders" table.
/// </summary>
[Table("Orders")]
public class Order : EntityBase
{
    /// <summary>Foreign key referencing the owning <see cref="User"/>'s Id.</summary>
    [ForeignKey(typeof(User))]
    public int UserId { get; set; }

    /// <summary>Total monetary amount for the order. Required at the database level.</summary>
    [NotNull]
    public decimal Total { get; set; }
}