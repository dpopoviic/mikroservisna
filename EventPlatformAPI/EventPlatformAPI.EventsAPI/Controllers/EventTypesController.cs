using EventPlatformAPI.DTO;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.EventsAPI.Controllers;

[ApiController]
[Route("api/event-types")]
public class EventTypesController : ControllerBase
{
    private readonly EventsDbContext _context;

    public EventTypesController(EventsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventTypeDto>>> GetAll()
    {
        var eventTypes = await _context.EventTypes
            .OrderBy(x => x.Name)
            .Select(x => new EventTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync();

        return Ok(eventTypes);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventTypeDto>> GetById(int id)
    {
        var eventType = await _context.EventTypes
            .Where(x => x.Id == id)
            .Select(x => new EventTypeDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .FirstOrDefaultAsync();

        return eventType is null ? NotFound() : Ok(eventType);
    }

    [HttpPost]
    public async Task<ActionResult<EventTypeDto>> Create(EventTypeDto request)
    {
        var entity = new EventType
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.EventTypes.Add(entity);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new EventTypeDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventTypeDto request)
    {
        var entity = await _context.EventTypes.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = request.Name;
        entity.Description = request.Description;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.EventTypes.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        _context.EventTypes.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
