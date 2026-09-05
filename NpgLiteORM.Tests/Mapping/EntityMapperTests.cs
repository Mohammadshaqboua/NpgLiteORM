using NpgLiteORM.Core.Mapping;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Mapping;

public class EntityMapperTests
{
    [Fact]
    public void MapToRow_ShouldReturnCorrectColumnNamesAndValues()
    {
        var mapper = new EntityMapper<User>();
        var user = new User { Name = "Ahmad", Email = "ahmad@test.com" };

        var row = mapper.MapToRow(user);

        Assert.Equal("Ahmad", row["full_name"]);
        Assert.Equal("ahmad@test.com", row["email"]);
    }
    
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
