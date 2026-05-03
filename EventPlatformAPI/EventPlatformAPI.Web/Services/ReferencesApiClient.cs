using System.Net.Http.Json;
using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services;

public class ReferencesApiClient : IReferencesApiClient
{
    private readonly HttpClient _httpClient;

    public ReferencesApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<LocationDto>> GetLocationsAsync() =>
        await _httpClient.GetFromJsonAsync<List<LocationDto>>("api/locations") ?? [];

    public async Task<LocationDto?> GetLocationByIdAsync(int id) =>
        await _httpClient.GetFromJsonAsync<LocationDto>($"api/locations/{id}");

    public async Task<bool> CreateLocationAsync(LocationDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/locations", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateLocationAsync(int id, LocationDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/locations/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLocationAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/locations/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<LecturerDto>> GetLecturersAsync() =>
        await _httpClient.GetFromJsonAsync<List<LecturerDto>>("api/lecturers") ?? [];

    public async Task<LecturerDto?> GetLecturerByIdAsync(int id) =>
        await _httpClient.GetFromJsonAsync<LecturerDto>($"api/lecturers/{id}");

    public async Task<bool> CreateLecturerAsync(LecturerDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/lecturers", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateLecturerAsync(int id, LecturerDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/lecturers/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLecturerAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/lecturers/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<ValidateReferencesResponseDto?> ValidateReferencesAsync(ValidateReferencesRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/references/validate", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ValidateReferencesResponseDto>();
        }
        return null;
    }
}

