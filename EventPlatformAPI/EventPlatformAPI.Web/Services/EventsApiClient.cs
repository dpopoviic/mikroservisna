using System.Net.Http.Json;
using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services;

public class EventsApiClient : IEventsApiClient
{
    private readonly HttpClient _httpClient;

    public EventsApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<EventSummaryDto>> GetEventsAsync() =>
        await _httpClient.GetFromJsonAsync<List<EventSummaryDto>>("api/events") ?? [];

    public async Task<EventDetailsDto?> GetEventByIdAsync(int id) =>
        await _httpClient.GetFromJsonAsync<EventDetailsDto>($"api/events/{id}");

    public async Task<bool> CreateEventAsync(EventCreateRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/events", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateEventAsync(int id, EventUpdateRequestDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/events/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/events/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<EventTypeDto>> GetEventTypesAsync() =>
        await _httpClient.GetFromJsonAsync<List<EventTypeDto>>("api/event-types") ?? [];

    public async Task<EventTypeDto?> GetEventTypeByIdAsync(int id) =>
        await _httpClient.GetFromJsonAsync<EventTypeDto>($"api/event-types/{id}");

    public async Task<bool> CreateEventTypeAsync(EventTypeDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/event-types", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateEventTypeAsync(int id, EventTypeDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/event-types/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEventTypeAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/event-types/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<EventLecturerDto>> GetEventLecturersAsync() =>
        await _httpClient.GetFromJsonAsync<List<EventLecturerDto>>("api/event-lecturers") ?? [];

    public async Task<EventLecturerDto?> GetEventLecturerByIdAsync(int id) =>
        await _httpClient.GetFromJsonAsync<EventLecturerDto>($"api/event-lecturers/{id}");

    public async Task<bool> CreateEventLecturerAsync(EventLecturerCreateRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/event-lecturers", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateEventLecturerAsync(int id, EventLecturerUpdateRequestDto request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/event-lecturers/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteEventLecturerAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/event-lecturers/{id}");
        return response.IsSuccessStatusCode;
    }
}
