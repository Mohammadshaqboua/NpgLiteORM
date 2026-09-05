using NpgLiteORM.Core.Data;
using NpgLiteORM.Core.Repositories;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Repositories;

public class RepositoryTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=npglite_db;Username=postgres;Password=postgres123";

    private PostgresConnectionFactory _connectionFactory = null!;
    private Repository<User> _repository = null!;

    public async Task InitializeAsync()
    {
        _connectionFactory = new PostgresConnectionFactory(ConnectionString);
        _repository = new Repository<User>(_connectionFactory);

        var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "TRUNCATE TABLE users RESTART IDENTITY CASCADE";
        command.ExecuteNonQuery();
        connection.Close();

        await Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_ShouldAssignGeneratedId()
    {
        // Arrange
        var user = new User { Name = "Ahmad", Email = "ahmad@test.com" };

        // Act
        await _repository.AddAsync(user);

        // Assert
        Assert.True(user.Id > 0);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var user = new User { Name = "Sara", Email = "sara@test.com" };
        await _repository.AddAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.Equal("Sara", result.Name);
        Assert.Equal("sara@test.com", result.Email);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllInsertedUsers()
    {
        // Arrange
        await _repository.AddAsync(new User { Name = "A", Email = "a@test.com" });
        await _repository.AddAsync(new User { Name = "B", Email = "b@test.com" });

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyExistingUser()
    {
        // Arrange
        var user = new User { Name = "Zaid", Email = "zaid@test.com" };
        await _repository.AddAsync(user);

        // Act
        user.Name = "Zaid Updated";
        await _repository.UpdateAsync(user);
        var updated = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.Equal("Zaid Updated", updated.Name);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser()
    {
        // Arrange
        var user = new User { Name = "ToDelete", Email = "delete@test.com" };
        await _repository.AddAsync(user);

        // Act
        await _repository.DeleteAsync(user.Id);

        // Assert
        await Assert.ThrowsAsync<NpgLiteORM.Core.Exceptions.EntityNotFoundException>(
            () => _repository.GetByIdAsync(user.Id));
    }
}