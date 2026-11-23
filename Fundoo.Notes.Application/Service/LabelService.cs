using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;

namespace Fundoo.Notes.Application.Services;

public class LabelService(
    ILabelRepository labelRepository) : ILabelService
{
    public async Task<Label> CreateLabelAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = ValidateName(name);
        var now = DateTime.UtcNow;

        var label = new Label
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = normalizedName,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await labelRepository.AddAsync(
            label,
            cancellationToken);

        return label;
    }

    public Task<IReadOnlyList<Label>> GetMyLabelsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return labelRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);
    }

    public Task<Label?> GetLabelByIdAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return labelRepository.GetByIdAsync(
            labelId,
            userId,
            cancellationToken);
    }

    public async Task<Label?> UpdateLabelAsync(
        Guid labelId,
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = ValidateName(name);

        var label = await labelRepository.GetByIdAsync(
            labelId,
            userId,
            cancellationToken);

        if (label is null)
            return null;

        label.Name = normalizedName;
        label.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await labelRepository.UpdateAsync(
            label,
            cancellationToken);

        return updated ? label : null;
    }

    public Task<bool> DeleteLabelAsync(
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return labelRepository.DeleteAsync(
            labelId,
            userId,
            cancellationToken);
    }

    public Task<bool> AttachLabelToNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return labelRepository.AttachLabelToNoteAsync(
            noteId,
            labelId,
            userId,
            cancellationToken);
    }

    public Task<bool> DetachLabelFromNoteAsync(
        Guid noteId,
        Guid labelId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return labelRepository.DetachLabelFromNoteAsync(
            noteId,
            labelId,
            userId,
            cancellationToken);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Label name cannot be empty.");

        var normalizedName = name.Trim();

        if (normalizedName.Length > 100)
            throw new ArgumentException(
                "Label name cannot exceed 100 characters.");

        return normalizedName;
    }
}
