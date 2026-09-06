using NpgLiteORM.Core.Abstract;
using NpgLiteORM.Core.Attributes;

namespace NpgLiteORM.Demo.Models;

/// <summary>
/// Sample entity showing typical attribute usage: a renamed column ([Column]),
/// a required field ([NotNull]), a length-bounded string ([MaxLength]), and a
/// uniqueness constraint ([Unique]). Maps to the "users" table.
/// </summary>
[Table("users")]
public class User : EntityBase
{
    /// <summary>The user's display name. Stored in the "full_name" column, required, max 50 characters.</summary>
    [Column("full_name"), NotNull, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The user's email address. Must be unique across all users, max 200 characters.</summary>
    [Column("email"), MaxLength(200), Unique]
    public string Email { get; set; } = string.Empty;
}