using EventPlatformAPI.DTO;
using EventPlatformAPI.ReferencesAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.ReferencesAPI.Controllers;

[ApiController]
[Route("api/references")]
public class ReferencesController : ControllerBase
{
    private readonly ReferenceDbContext _context;

    public ReferencesController(ReferenceDbContext context)
    {
        _context = context;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidateReferencesResponseDto>> Validate(ValidateReferencesRequestDto request)
    {
        var response = new ValidateReferencesResponseDto
        {
            LocationExists = !request.LocationId.HasValue || await _context.Locations.AnyAsync(x => x.Id == request.LocationId.Value)
        };

        if (request.LecturerIds.Count > 0)
        {
            var existingIds = await _context.Lecturers
                .Where(x => request.LecturerIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            response.MissingLecturerIds = request.LecturerIds.Except(existingIds).OrderBy(x => x).ToList();
        }

        if (!response.LocationExists)
        {
            response.Errors.Add("Prosleđeni LocationId ne postoji.");
        }

        if (response.MissingLecturerIds.Count > 0)
        {
            response.Errors.Add("Neki LecturerId ne postoje.");
        }

        response.IsValid = response.Errors.Count == 0;
        return Ok(response);
    }
}
