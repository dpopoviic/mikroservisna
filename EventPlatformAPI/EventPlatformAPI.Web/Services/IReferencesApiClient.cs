using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services;

public interface IReferencesApiClient
{
    Task<List<LocationDto>> GetLocationsAsync();
    Task<LocationDto?> GetLocationByIdAsync(int id);
    Task<bool> CreateLocationAsync(LocationDto request);
    Task<bool> UpdateLocationAsync(int id, LocationDto request);
    Task<bool> DeleteLocationAsync(int id);

    Task<List<LecturerDto>> GetLecturersAsync();
    Task<LecturerDto?> GetLecturerByIdAsync(int id);
    Task<bool> CreateLecturerAsync(LecturerDto request);
    Task<bool> UpdateLecturerAsync(int id, LecturerDto request);
    Task<bool> DeleteLecturerAsync(int id);

    Task<ValidateReferencesResponseDto?> ValidateReferencesAsync(ValidateReferencesRequestDto request);
}

