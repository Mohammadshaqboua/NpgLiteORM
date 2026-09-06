using NpgLiteORM.Core.Data;
using NpgLiteORM.Core.Repositories;
using NpgLiteORM.Demo.Models;
using Xunit;

namespace NpgLiteORM.Tests.Repositories;

/// <summary>
/// Integration tests for Repository&lt;User&gt; against a real PostgreSQL instance
/// (see docker-compose.yml / CI's service container). Every test gets a clean,
/// empty "users" table courtesy of <see cref="InitializeAsync"/>.
/// </summary>
public class RepositoryTests : IAsyncLifetime
{
    private const string ConnectionString =
        "Host=localhost;Port=5433;Database=npglite_db;Username=postgres;Password=postgres123";

    private PostgresConnectionFactory _connectionFactory = null!;
    private Repository<User> _repository = null!;

    /// <summary>
    /// Runs before every test: creates the "users" table if it doesn't exist yet
    /// (so the suite works on a brand-new database, not just one that already ran
    /// migrations) and truncates it, guaranteeing each test starts from zero rows.
    /// </summary>
    public async Task InitializeAsync()
    {
        _connectionFactory = new PostgresConnectionFactory(ConnectionString);
        _repository = new Repository<User>(_connectionFactory);

        var connection = _connectionFactory.CreateConnection();
        connection.Open();

        var schemaBuilder = new NpgLiteORM.Core.Mapping.SchemaBuilder();
        var createTableSql = schemaBuilder.BuildCreateTableSql<User>();

        using (var createCommand = connection.CreateCommand())
        {
            createCommand.CommandText = createTableSql;
            createCommand.ExecuteNonQuery();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "TRUNCATE TABLE users RESTART IDENTITY CASCADE";
        command.ExecuteNonQuery();

        connection.Close();

        await Task.CompletedTask;
    }

    /// <summary>No per-test teardown needed — the next test's InitializeAsync truncates the table again.</summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>AddAsync should populate the entity's auto-generated Id (from the SERIAL column) after inserting.</summary>
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

    /// <summary>GetByIdAsync should return the exact row just inserted, matched by its generated Id.</summary>
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

    /// <summary>GetAllAsync should return every row inserted since the table was truncated for this test.</summary>
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

    /// <summary>UpdateAsync should persist a changed property value, verified by re-fetching the row afterwards.</summary>
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

    /// <summary>DeleteAsync should remove the row so a subsequent GetByIdAsync for the same Id throws EntityNotFoundException.</summary>
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
