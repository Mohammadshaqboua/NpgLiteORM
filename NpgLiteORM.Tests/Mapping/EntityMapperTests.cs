using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Mapping;

/// <summary>Covers EntityMapper's two directions: object → row (MapToRow) and row → object (MapToEntity).</summary>
public class EntityMapperTests
{
    /// <summary>MapToRow should key the resulting dictionary by SQL column name (e.g. "full_name"), not the C# property name ("Name").</summary>
    [Fact]
    public void MapToRow_ShouldReturnCorrectColumnNamesAndValues()
    {
        var mapper = new EntityMapper<User>();
        var user = new User { Name = "Ahmad", Email = "ahmad@test.com" };

        var row = mapper.MapToRow(user);

        Assert.Equal("Ahmad", row["full_name"]);
        Assert.Equal("ahmad@test.com", row["email"]);
    }

    /// <summary>MapToEntity should read every mapped column off a fake row (case-insensitively, via FakeDataRecord) and populate the resulting entity's properties.</summary>
    [Fact]
    public void MapToEntity_ShouldBuildUserFromFakeRow()
    {
        // Arrange
        var mapper = new EntityMapper<User>();
        var fakeRow = new FakeDataRecord(new Dictionary<string, object>
        {
            { "Id", 1 },
            { "full_name", "Sara" },
            { "Email", "sara@test.com" },
            { "CreatedAt", DateTime.UtcNow },
            { "UpdatedAt", DateTime.UtcNow }
        });

        // Act
        var user = mapper.MapToEntity(fakeRow);

        // Assert
        Assert.Equal(1, user.Id);
        Assert.Equal("Sara", user.Name);
        Assert.Equal("sara@test.com", user.Email);
    }
}