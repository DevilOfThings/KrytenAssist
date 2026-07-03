using System.Text.Json;
using KrytenAssist.Application.Abstractions.Persistence; 
using KrytenAssist.Core.Entities;
using Microsoft.Data.Sqlite;

namespace KrytenAssist.Infrastructure.Persistence;

public sealed class SqlitePromptCardRepository : IPromptCardRepository
{
    private readonly string _connectionString;

    public SqlitePromptCardRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task AddAsync(PromptCard promptCard, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO PromptCards
                              (
                                  Id,
                                  Title,
                                  Category,
                                  Description,
                                  PromptText,
                                  Tags,
                                  CreatedAt,
                                  UpdatedAt
                              )
                              VALUES
                              (
                                  $id,
                                  $title,
                                  $category,
                                  $description,
                                  $promptText,
                                  $tags,
                                  $createdAt,
                                  $updatedAt
                              );
                              """;

        AddPromptCardParameters(command, promptCard);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PromptCard>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT
                                  Id,
                                  Title,
                                  Category,
                                  Description,
                                  PromptText,
                                  Tags,
                                  CreatedAt,
                                  UpdatedAt
                              FROM PromptCards
                              ORDER BY CreatedAt DESC;
                              """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var promptCards = new List<PromptCard>();

        while (await reader.ReadAsync(cancellationToken))
        {
            promptCards.Add(ReadPromptCard(reader));
        }

        return promptCards;
    }

    public async Task<PromptCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT
                                  Id,
                                  Title,
                                  Category,
                                  Description,
                                  PromptText,
                                  Tags,
                                  CreatedAt,
                                  UpdatedAt
                              FROM PromptCards
                              WHERE Id = $id;
                              """;

        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadPromptCard(reader);
    }

    public async Task<bool> UpdateAsync(PromptCard promptCard, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE PromptCards
                              SET
                                  Title = $title,
                                  Category = $category,
                                  Description = $description,
                                  PromptText = $promptText,
                                  Tags = $tags,
                                  UpdatedAt = $updatedAt
                              WHERE Id = $id;
                              """;

        AddPromptCardParameters(command, promptCard);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              DELETE FROM PromptCards
                              WHERE Id = $id;
                              """;

        command.Parameters.AddWithValue("$id", id.ToString());

        var deleted = await command.ExecuteNonQueryAsync(cancellationToken);
        
        return deleted > 0;
    }

    private static void AddPromptCardParameters(SqliteCommand command, PromptCard promptCard)
    {
        command.Parameters.AddWithValue("$id", promptCard.Id.ToString());
        command.Parameters.AddWithValue("$title", promptCard.Title);
        command.Parameters.AddWithValue("$category", promptCard.Category);
        command.Parameters.AddWithValue("$description", (object?)promptCard.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$promptText", promptCard.PromptText);
        command.Parameters.AddWithValue("$tags", JsonSerializer.Serialize(promptCard.Tags));
        command.Parameters.AddWithValue("$createdAt", promptCard.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updatedAt", promptCard.UpdatedAt.ToString("O"));
    }

    private static PromptCard ReadPromptCard(SqliteDataReader reader)
    {
        var tags = JsonSerializer.Deserialize<IReadOnlyCollection<string>>(reader.GetString(5))
                   ?? Array.Empty<string>();

        return new PromptCard
        {
            Id = Guid.Parse(reader.GetString(0)),
            Title = reader.GetString(1),
            Category = reader.GetString(2),
            Description = reader.IsDBNull(3) ? null : reader.GetString(3),
            PromptText = reader.GetString(4),
            Tags = tags,
            CreatedAt = DateTimeOffset.Parse(reader.GetString(6)),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(7))
        };
    }
}