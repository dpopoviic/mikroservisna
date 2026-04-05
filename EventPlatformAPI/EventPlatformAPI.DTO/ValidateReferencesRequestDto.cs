namespace EventPlatformAPI.DTO;

public class ValidateReferencesRequestDto
{
    public int? TypeId { get; set; }
    public int? LocationId { get; set; }
    public ICollection<int> LecturerIds { get; set; } = new List<int>();
}
