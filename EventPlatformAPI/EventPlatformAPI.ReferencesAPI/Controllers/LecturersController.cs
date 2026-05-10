using EventPlatformAPI.DTO;
using EventPlatformAPI.Messages.IntegrationEvents;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventPlatformAPI.ReferencesAPI.Controllers;

[ApiController]
[Route("api/lecturers")]
public class LecturersController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public LecturersController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LecturerDto>>> GetAll()
    {
        var lecturers = await _context.Lecturers
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new LecturerDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Title = x.Title,
                Field = x.Field
            })
            .ToListAsync();

        return Ok(lecturers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LecturerDto>> GetById(int id)
    {
        var lecturer = await _context.Lecturers
            .Where(x => x.Id == id)
            .Select(x => new LecturerDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Title = x.Title,
                Field = x.Field
            })
            .FirstOrDefaultAsync();

        return lecturer is null ? NotFound() : Ok(lecturer);
    }

    [HttpPost]
    public async Task<ActionResult<LecturerDto>> Create(LecturerDto request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var entity = new Lecturer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Title = request.Title,
                Field = request.Field
            };

            _context.Lecturers.Add(entity);
            await _context.SaveChangesAsync();

            var evt = new LecturerCreatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LecturerId = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Title = entity.Title
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "lecturer.created",
                Type = nameof(LecturerCreatedEvent),
                Payload = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new LecturerDto
            {
                Id = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Title = entity.Title,
                Field = entity.Field
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LecturerDto request)
    {
        var entity = await _context.Lecturers.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            entity.FirstName = request.FirstName;
            entity.LastName = request.LastName;
            entity.Title = request.Title;
            entity.Field = request.Field;

            await _context.SaveChangesAsync();

            var evt = new LecturerUpdatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LecturerId = entity.Id,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Title = entity.Title
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "lecturer.updated",
                Type = nameof(LecturerUpdatedEvent),
                Payload = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Lecturers.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Lecturers.Remove(entity);
            await _context.SaveChangesAsync();

            var evt = new LecturerDeletedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LecturerId = entity.Id
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "lecturer.deleted",
                Type = nameof(LecturerDeletedEvent),
                Payload = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return NoContent();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
