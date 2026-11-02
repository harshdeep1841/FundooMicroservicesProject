using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Npgsql;
using NpgsqlTypes;

namespace Fundoo.Notes.Infrastructure.Repositories;

public class NoteRepository(NpgsqlDataSource dataSource)
    : INoteRepository
{
    public async Task<Note?> GetByIdAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id, title, content,
                   is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE id = @id AND user_id = @userId;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", noteId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapNote(reader);
    }

    public Task<IReadOnlyList<Note>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => GetNotesAsync(
            """
            SELECT id, user_id, title, content,
                   is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_archived = FALSE
              AND is_trashed = FALSE
            ORDER BY updated_at_utc DESC;
            """,
            userId,
            cancellationToken);

    public Task<IReadOnlyList<Note>> GetArchivedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => GetNotesAsync(
            """
            SELECT id, user_id, title, content,
                   is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_archived = TRUE
              AND is_trashed = FALSE
            ORDER BY updated_at_utc DESC;
            """,
            userId,
            cancellationToken);

    public Task<IReadOnlyList<Note>> GetTrashedByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
        => GetNotesAsync(
            """
            SELECT id, user_id, title, content,
                   is_archived, is_trashed,
                   created_at_utc, updated_at_utc
            FROM notes
            WHERE user_id = @userId
              AND is_trashed = TRUE
            ORDER BY updated_at_utc DESC;
            """,
            userId,
            cancellationToken);

    private async Task<IReadOnlyList<Note>> GetNotesAsync(
        string sql,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var notes = new List<Note>();

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            notes.Add(MapNote(reader));
        }

        return notes;
    }

    public async Task AddAsync(
        Note note,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO notes (
                id, user_id, title, content,
                is_archived, is_trashed,
                created_at_utc, updated_at_utc
            )
            VALUES (
                @id, @userId, @title, @content,
                @isArchived, @isTrashed,
                @createdAtUtc, @updatedAtUtc
            );
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", note.Id);
        command.Parameters.AddWithValue("userId", note.UserId);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("content", note.Content);
        command.Parameters.AddWithValue("isArchived", note.IsArchived);
        command.Parameters.AddWithValue("isTrashed", note.IsTrashed);
        command.Parameters.AddWithValue("createdAtUtc", note.CreatedAtUtc);
        command.Parameters.AddWithValue("updatedAtUtc", note.UpdatedAtUtc);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Note note,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET title = @title,
                content = @content,
                updated_at_utc = @updatedAtUtc
            WHERE id = @id
              AND user_id = @userId
              AND is_trashed = FALSE;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", note.Id);
        command.Parameters.AddWithValue("userId", note.UserId);
        command.Parameters.AddWithValue("title", note.Title);
        command.Parameters.AddWithValue("content", note.Content);
        command.Parameters.AddWithValue("updatedAtUtc", note.UpdatedAtUtc);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetArchiveStateAsync(
        Guid noteId,
        Guid userId,
        bool isArchived,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET is_archived = @isArchived,
                updated_at_utc = NOW()
            WHERE id = @id
              AND user_id = @userId
              AND is_trashed = FALSE;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", noteId);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("isArchived", isArchived);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> SetTrashStateAsync(
        Guid noteId,
        Guid userId,
        bool isTrashed,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE notes
            SET is_trashed = @isTrashed,
                is_archived = CASE
                    WHEN @isTrashed THEN FALSE
                    ELSE is_archived
                END,
                updated_at_utc = NOW()
            WHERE id = @id
              AND user_id = @userId
              AND (is_trashed = FALSE OR @isTrashed = FALSE);
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", noteId);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("isTrashed", isTrashed);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> PermanentlyDeleteAsync(
        Guid noteId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM notes
            WHERE id = @id
              AND user_id = @userId
              AND is_trashed = TRUE;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", noteId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Note MapNote(NpgsqlDataReader reader)
    {
        return new Note
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Title = reader.GetString(2),
            Content = reader.GetString(3),
            IsArchived = reader.GetBoolean(4),
            IsTrashed = reader.GetBoolean(5),
            CreatedAtUtc = reader.GetFieldValue<DateTime>(6),
            UpdatedAtUtc = reader.GetFieldValue<DateTime>(7)
        };
    }
    
    
    
}