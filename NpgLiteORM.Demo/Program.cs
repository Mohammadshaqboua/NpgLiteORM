using Npgsql;

string connectionString = "Host=localhost;Port=5433;Database=npglite_db;Username=postgres;Password=postgres123";

await using var connection = new NpgsqlConnection(connectionString);

Console.WriteLine("Attempting to connect to the database...");

await connection.OpenAsync();

Console.WriteLine("Connection successful! ✅");

await using var command = new NpgsqlCommand("SELECT version();", connection);
var result = await command.ExecuteScalarAsync();

Console.WriteLine($"copy PostgreSQL: {result}");