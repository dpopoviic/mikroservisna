using EventPlatformAPI.DTO;
using EventPlatformAPI.Messages.IntegrationEvents;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Text.Json;

namespace EventPlatformAPI.ReferencesAPI.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ReferenceDbContext _context;
    //private static int _counter = 0;

    public LocationsController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
    {
        //_counter++;

        //if (_counter % 4 != 0)
        //    return StatusCode(500, "Simulated server error");

        var locations = await _context.Locations
            .OrderBy(x => x.Name)
            .Select(x => new LocationDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                Capacity = x.Capacity
            })
            .ToListAsync();

        return Ok(locations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LocationDto>> GetById(int id)
    {
        var location = await _context.Locations
            .Where(x => x.Id == id)
            .Select(x => new LocationDto
            {
                Id = x.Id,
                Name = x.Name,
                Address = x.Address,
                Capacity = x.Capacity
            })
            .FirstOrDefaultAsync();

        return location is null ? NotFound() : Ok(location);
    }

    [HttpPost]
    public async Task<ActionResult<LocationDto>> Create(LocationDto request)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var entity = new Location
            {
                Name = request.Name,
                Address = request.Address,
                Capacity = request.Capacity
            };

            _context.Locations.Add(entity);
            await _context.SaveChangesAsync();

            var evt = new LocationCreatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LocationId = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Capacity = entity.Capacity
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "location.created",
                Type = nameof(LocationCreatedEvent),
                Payload = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            };

            _context.OutboxMessages.Add(outbox);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new LocationDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Capacity = entity.Capacity
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
    public async Task<IActionResult> Update(int id, LocationDto request)
    {
        var entity = await _context.Locations.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            entity.Name = request.Name;
            entity.Address = request.Address;
            entity.Capacity = request.Capacity;

            await _context.SaveChangesAsync();

            var evt = new LocationUpdatedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LocationId = entity.Id,
                Name = entity.Name,
                Address = entity.Address,
                Capacity = entity.Capacity
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "location.updated",
                Type = nameof(LocationUpdatedEvent),
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
        var entity = await _context.Locations.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Locations.Remove(entity);
            await _context.SaveChangesAsync();

            var evt = new LocationDeletedEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                LocationId = entity.Id
            };

            var outbox = new OutboxMessage
            {
                MessageId = Guid.NewGuid(),
                Destination = "location.deleted",
                Type = nameof(LocationDeletedEvent),
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
