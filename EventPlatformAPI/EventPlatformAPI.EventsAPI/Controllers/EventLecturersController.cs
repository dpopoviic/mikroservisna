using EventPlatformAPI.DTO;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.EventsAPI.Controllers;

[ApiController]
[Route("api/event-lecturers")]
public class EventLecturersController : ControllerBase
{
    private readonly EventsDbContext _context;

    public EventLecturersController(EventsDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventLecturerDto>>> GetAll()
    {
        var slots = await _context.EventLecturers
            .OrderBy(x => x.DateTime)
            .Select(x => new EventLecturerDto
            {
                Id = x.Id,
                EventId = x.EventId,
                LecturerId = x.LecturerId,
                DateTime = x.DateTime,
                Theme = x.Theme
            })
            .ToListAsync();

        return Ok(slots);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventLecturerDto>> GetById(int id)
    {
        var slot = await _context.EventLecturers
            .Where(x => x.Id == id)
            .Select(x => new EventLecturerDto
            {
                Id = x.Id,
                EventId = x.EventId,
                LecturerId = x.LecturerId,
                DateTime = x.DateTime,
                Theme = x.Theme
            })
            .FirstOrDefaultAsync();

        return slot is null ? NotFound() : Ok(slot);
    }

    [HttpPost]
    public async Task<ActionResult<EventLecturerDto>> Create(EventLecturerCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (!await _context.Events.AnyAsync(x => x.Id == request.EventId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.EventId), "Prosleđeni EventId ne postoji.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }
        //da li treba proveriti postoji li predavac sa datim id-jem?
        var entity = new EventLecturer
        {
            EventId = request.EventId,
            LecturerId = request.LecturerId,
            DateTime = request.DateTime,
            Theme = request.Theme
        };

        _context.EventLecturers.Add(entity);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Termin predavača za isti događaj već postoji." });
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new EventLecturerDto
        {
            Id = entity.Id,
            EventId = entity.EventId,
            LecturerId = entity.LecturerId,
            DateTime = entity.DateTime,
            Theme = entity.Theme
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventLecturerUpdateRequestDto request, CancellationToken cancellationToken)
    {
        var entity = await _context.EventLecturers.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        if (!await _context.Events.AnyAsync(x => x.Id == request.EventId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.EventId), "Prosleđeni EventId ne postoji.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        entity.EventId = request.EventId;
        entity.LecturerId = request.LecturerId;
        entity.DateTime = request.DateTime;
        entity.Theme = request.Theme;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Termin predavača za isti događaj već postoji." });
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var entity = await _context.EventLecturers.FindAsync([id], cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        _context.EventLecturers.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
