using System.Security.Claims;
using Fundoo.Notes.Api.Contracts;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fundoo.Notes.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/notes")]
public class NotesController(
    INoteService noteService, ILogger<NotesController> logger) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> CreateNote(
        CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var note = await noteService.CreateNoteAsync(
            userId,
            request.Title,
            request.Content,
            cancellationToken);

        var response = MapToResponse(note);

        return CreatedAtAction(
            nameof(GetNoteById),
            new { id = note.Id },
            response);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotes(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var notes = await noteService.GetMyNotesAsync(
            userId, cancellationToken);

        var response = notes.Select(MapToResponse).ToList();
        logger.LogInformation("Testing correlation ID logging.");
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetNoteById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var note = await noteService.GetNoteByIdAsync(
            id, userId, cancellationToken);

        if (note is null)
            return NotFound();

        return Ok(MapToResponse(note));
    }

    private static NoteResponse MapToResponse(Note note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            UserId = note.UserId,
            Title = note.Title,
            Content = note.Content,
            IsArchived = note.IsArchived,
            IsTrashed = note.IsTrashed,
            CreatedAtUtc = note.CreatedAtUtc,
            UpdatedAtUtc = note.UpdatedAtUtc
        };
    }

    [HttpPost("{id:guid}/collaborators")]
    public async Task<IActionResult> AddCollaborator(
        Guid id,
        [FromBody] AddCollaboratorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
            return Unauthorized();

        var collaboratorId = await noteService.AddCollaboratorAsync(
            id,
            ownerId,
            request.Email,
            cancellationToken);

        if (collaboratorId is null)
        {
            return BadRequest(new
            {
                message = "The note, registered user, or collaborator membership could not be found."
            });
        }

        return CreatedAtAction(
            nameof(GetCollaborators),
            new { id },
            new CollaboratorResponse(collaboratorId.Value));
    }

    [HttpGet("{id:guid}/collaborators")]
    public async Task<IActionResult> GetCollaborators(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
            return Unauthorized();

        var collaborators = await noteService.GetCollaboratorsAsync(
            id,
            ownerId,
            cancellationToken);

        if (collaborators is null)
            return NotFound();

        return Ok(collaborators
            .Select(userId => new CollaboratorResponse(userId))
            .ToArray());
    }

    [HttpDelete("{id:guid}/collaborators/{collaboratorId:guid}")]
    public async Task<IActionResult> RemoveCollaborator(
        Guid id,
        Guid collaboratorId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var ownerId))
            return Unauthorized();

        var removed = await noteService.RemoveCollaboratorAsync(
            id,
            ownerId,
            collaboratorId,
            cancellationToken);

        return removed ? NoContent() : NotFound();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }
    
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateNote(
        Guid id,
        UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var note = await noteService.UpdateNoteAsync(
            id,
            userId,
            request.Title,
            request.Content,
            cancellationToken);

        if (note is null)
            return NotFound();

        return Ok(MapToResponse(note));
    }
    
    
    
    [HttpPut("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await noteService.ArchiveNoteAsync(
            id, userId, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id:guid}/unarchive")]
    public async Task<IActionResult> UnarchiveNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await noteService.UnarchiveNoteAsync(
            id, userId, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id:guid}/trash")]
    public async Task<IActionResult> TrashNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await noteService.TrashNoteAsync(
            id, userId, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id:guid}/restore")]
    public async Task<IActionResult> RestoreNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await noteService.RestoreNoteAsync(
            id, userId, cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }
    
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> PermanentlyDeleteNote(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var success = await noteService.PermanentlyDeleteNoteAsync(
            id,
            userId,
            cancellationToken);

        if (!success)
            return NotFound();

        return NoContent();
    }
    
    
    [HttpGet("archived")]
    public async Task<IActionResult> GetArchivedNotes(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var notes = await noteService.GetArchivedNotesAsync(
            userId, cancellationToken);

        return Ok(notes.Select(MapToResponse).ToList());
    }

    [HttpGet("trash")]
    public async Task<IActionResult> GetTrashedNotes(
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var notes = await noteService.GetTrashedNotesAsync(
            userId, cancellationToken);

        return Ok(notes.Select(MapToResponse).ToList());
    }

}
