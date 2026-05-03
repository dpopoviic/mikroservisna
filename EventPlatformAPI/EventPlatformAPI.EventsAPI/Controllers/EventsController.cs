using EventPlatformAPI.DTO;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.EventsAPI.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly EventsDbContext _context;

    public EventsController(EventsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventSummaryDto>>> GetAll()
    {
        var events = await _context.Events
            .OrderBy(x => x.DateTime)
            .Select(x => new EventSummaryDto
            {
                Id = x.Id,
                Name = x.Name,
                DateTime = x.DateTime,
                DurationInHours = x.DurationInHours,
                Price = x.Price,
                TypeId = x.TypeId,
                LocationId = x.LocationId
            })
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDetailsDto>> GetById(int id)
    {
        var result = await _context.Events
            .Include(x => x.Type)
            .Include(x => x.EventLecturers)
            .Where(x => x.Id == id)
            .Select(x => new EventDetailsDto
            {
                Id = x.Id,
                Name = x.Name,
                Agenda = x.Agenda,
                DateTime = x.DateTime,
                DurationInHours = x.DurationInHours,
                Price = x.Price,
                TypeId = x.TypeId,
                LocationId = x.LocationId,
                Type = x.Type == null
                    ? null
                    : new EventTypeDto
                    {
                        Id = x.Type.Id,
                        Name = x.Type.Name,
                        Description = x.Type.Description
                    },
                EventLecturers = x.EventLecturers
                    .OrderBy(e => e.DateTime)
                    .Select(e => new EventLecturerDto
                    {
                        Id = e.Id,
                        EventId = e.EventId,
                        LecturerId = e.LecturerId,
                        DateTime = e.DateTime,
                        Theme = e.Theme
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<EventDetailsDto>> Create(EventCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (request.DurationInHours <= 0)
        {
            ModelState.AddModelError(nameof(request.DurationInHours), "Trajanje mora biti veće od 0.");
        }

        if (request.Price < 0)
        {
            ModelState.AddModelError(nameof(request.Price), "Cena ne može biti negativna.");
        }

        if (!await _context.EventTypes.AnyAsync(x => x.Id == request.TypeId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.TypeId), "Prosleđeni TypeId ne postoji.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = new Event
        {
            Name = request.Name,
            Agenda = request.Agenda,
            DateTime = request.DateTime,
            DurationInHours = request.DurationInHours,
            Price = request.Price,
            TypeId = request.TypeId,
            LocationId = request.LocationId
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new EventDetailsDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Agenda = entity.Agenda,
            DateTime = entity.DateTime,
            DurationInHours = entity.DurationInHours,
            Price = entity.Price,
            TypeId = entity.TypeId,
            LocationId = entity.LocationId
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventUpdateRequestDto request, CancellationToken cancellationToken)
    {
        if (request.DurationInHours <= 0)
        {
            ModelState.AddModelError(nameof(request.DurationInHours), "Trajanje mora biti veće od 0.");
        }

        if (request.Price < 0)
        {
            ModelState.AddModelError(nameof(request.Price), "Cena ne može biti negativna.");
        }

        if (!await _context.EventTypes.AnyAsync(x => x.Id == request.TypeId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.TypeId), "Prosleđeni TypeId ne postoji.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var entity = await _context.Events.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = request.Name;
        entity.Agenda = request.Agenda;
        entity.DateTime = request.DateTime;
        entity.DurationInHours = request.DurationInHours;
        entity.Price = request.Price;
        entity.TypeId = request.TypeId;
        entity.LocationId = request.LocationId;

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.Events.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
