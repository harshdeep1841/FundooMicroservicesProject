
using System.Security.Claims;
using Fundoo.Notes.Api.Contracts;
using Fundoo.Notes.Application.Interfaces;
using Fundoo.Notes.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace Fundoo.Notes.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/labels")]
public class LabelsController(
    ILabelService labelService,
    ILogger<LabelsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateLabel(
        CreateLabelRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var label = await labelService.CreateLabelAsync(
                userId,
                request.Name,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetLabelById),
                new { id = label.Id },
                MapToResponse(label));
        }
        catch (PostgresException ex) when (
            ex.SqlState == "23505")
        {
            return Conflict(new
            {
                message = "A label with this name already exists."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyLabels(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var labels = await labelService.GetMyLabelsAsync(
            userId,
            cancellationToken);

        return Ok(labels.Select(MapToResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLabelById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var label = await labelService.GetLabelByIdAsync(
            id,
            userId,
            cancellationToken);

        if (label is null)
            return NotFound();

        return Ok(MapToResponse(label));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLabel(
        Guid id,
        UpdateLabelRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var label = await labelService.UpdateLabelAsync(
                id,
                userId,
                request.Name,
                cancellationToken);

            if (label is null)
                return NotFound();

            return Ok(MapToResponse(label));
        }
        catch (PostgresException ex) when (
            ex.SqlState == "23505")
        {
            return Conflict(new
            {
                message = "A label with this name already exists."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLabel(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var deleted = await labelService.DeleteLabelAsync(
            id,
            userId,
            cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("/api/notes/{noteId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> AttachLabelToNote(
        Guid noteId,
        Guid labelId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var attached = await labelService.AttachLabelToNoteAsync(
            noteId,
            labelId,
            userId,
            cancellationToken);

        if (!attached)
            return BadRequest(new
            {
                message = "The note or label does not exist, " +
                          "does not belong to you, or is already attached."
            });

        return NoContent();
    }

    [HttpDelete("/api/notes/{noteId:guid}/labels/{labelId:guid}")]
    public async Task<IActionResult> DetachLabelFromNote(
        Guid noteId,
        Guid labelId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var detached = await labelService.DetachLabelFromNoteAsync(
            noteId,
            labelId,
            userId,
            cancellationToken);

        if (!detached)
            return NotFound();

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue = User.FindFirst("sub")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userIdValue, out userId);
    }

    private static LabelResponse MapToResponse(Label label)
    {
        return new LabelResponse
        {
            Id = label.Id,
            UserId = label.UserId,
            Name = label.Name,
            CreatedAtUtc = label.CreatedAtUtc,
            UpdatedAtUtc = label.UpdatedAtUtc
        };
    }
}
