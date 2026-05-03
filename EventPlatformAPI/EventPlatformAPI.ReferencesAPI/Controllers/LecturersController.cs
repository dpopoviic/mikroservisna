using EventPlatformAPI.DTO;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var entity = new Lecturer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Title = request.Title,
            Field = request.Field
        };

        _context.Lecturers.Add(entity);
        await _context.SaveChangesAsync();

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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LecturerDto request)
    {
        var entity = await _context.Lecturers.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.FirstName = request.FirstName;
        entity.LastName = request.LastName;
        entity.Title = request.Title;
        entity.Field = request.Field;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Lecturers.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        _context.Lecturers.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
