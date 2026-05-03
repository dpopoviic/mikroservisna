using EventPlatformAPI.DTO;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.ReferencesAPI.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public LocationsController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LocationDto>>> GetAll()
    {
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
        var entity = new Location
        {
            Name = request.Name,
            Address = request.Address,
            Capacity = request.Capacity
        };

        _context.Locations.Add(entity);
        await _context.SaveChangesAsync();

        var response = new LocationDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Address = entity.Address,
            Capacity = entity.Capacity
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LocationDto request)
    {
        var entity = await _context.Locations.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        entity.Name = request.Name;
        entity.Address = request.Address;
        entity.Capacity = request.Capacity;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _context.Locations.FindAsync(id);
        if (entity is null)
        {
            return NotFound();
        }

        _context.Locations.Remove(entity);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
