using Microsoft.Data.Sqlite;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Initialise()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
                               CREATE TABLE IF NOT EXISTS PromptCards
                               (
                                   Id TEXT PRIMARY KEY,
                                   Title TEXT NOT NULL,
                                   Category TEXT NOT NULL,
                                   Description TEXT NULL,
                                   PromptText TEXT NOT NULL,
                                   Tags TEXT NOT NULL,
                                   CreatedAt TEXT NOT NULL,
                                   UpdatedAt TEXT NOT NULL
                               );
                               """;

        command.ExecuteNonQuery();
    }
}