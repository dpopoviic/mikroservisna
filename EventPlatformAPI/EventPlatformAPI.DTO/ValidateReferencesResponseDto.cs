namespace EventPlatformAPI.DTO;

public class ValidateReferencesResponseDto
{
    public bool IsValid { get; set; }
    public bool TypeExists { get; set; }
    public bool LocationExists { get; set; }
    public ICollection<int> MissingLecturerIds { get; set; } = new List<int>();
    public ICollection<string> Errors { get; set; } = new List<string>();
}
