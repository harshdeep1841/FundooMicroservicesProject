using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Npgsql;

namespace Fundoo.Notes.Infrastructure.Repositories;

public class LabelRepository(NpgsqlDataSource dataSource)
    : ILabelRepository
{
    public async Task<Label?> GetByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id, name,
                   created_at_utc, updated_at_utc
            FROM labels
            WHERE id = @id
              AND user_id = @userId;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", labelId);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return MapLabel(reader);
    }

    public async Task<IReadOnlyList<Label>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, user_id, name,
                   created_at_utc, updated_at_utc
            FROM labels
            WHERE user_id = @userId
            ORDER BY created_at_utc DESC;
            """;

        var labels = new List<Label>();

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("userId", userId);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            labels.Add(MapLabel(reader));
        }

        return labels;
    }

    public async Task AddAsync(
        Label label,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO labels (
                id,
                user_id,
                name,
                created_at_utc,
                updated_at_utc
            )
            VALUES (
                @id,
                @userId,
                @name,
                @createdAtUtc,
                @updatedAtUtc
            );
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", label.Id);
        command.Parameters.AddWithValue("userId", label.UserId);
        command.Parameters.AddWithValue("name", label.Name);
        command.Parameters.AddWithValue(
            "createdAtUtc", label.CreatedAtUtc);
        command.Parameters.AddWithValue(
            "updatedAtUtc", label.UpdatedAtUtc);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Label label,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE labels
            SET name = @name,
                updated_at_utc = @updatedAtUtc
            WHERE id = @id
              AND user_id = @userId;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", label.Id);
        command.Parameters.AddWithValue("userId", label.UserId);
        command.Parameters.AddWithValue("name", label.Name);
        command.Parameters.AddWithValue(
            "updatedAtUtc", label.UpdatedAtUtc);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM labels
            WHERE id = @id
              AND user_id = @userId;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("id", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> AttachLabelToNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO note_labels (note_id, label_id)
            SELECT n.id, l.id
            FROM notes n
            INNER JOIN labels l ON l.id = @labelId
            WHERE n.id = @noteId
              AND n.user_id = @userId
              AND l.user_id = @userId
            ON CONFLICT (note_id, label_id) DO NOTHING;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DetachLabelFromNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM note_labels nl
            USING notes n, labels l
            WHERE nl.note_id = n.id
              AND nl.label_id = l.id
              AND nl.note_id = @noteId
              AND nl.label_id = @labelId
              AND n.user_id = @userId
              AND l.user_id = @userId;
            """;

        await using var connection =
            await dataSource.OpenConnectionAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("noteId", noteId);
        command.Parameters.AddWithValue("labelId", labelId);
        command.Parameters.AddWithValue("userId", userId);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static Label MapLabel(NpgsqlDataReader reader)
    {
        return new Label
        {
            Id = reader.GetGuid(0),
            UserId = reader.GetGuid(1),
            Name = reader.GetString(2),
            CreatedAtUtc = reader.GetFieldValue<DateTime>(3),
            UpdatedAtUtc = reader.GetFieldValue<DateTime>(4)
        };
    }
}
