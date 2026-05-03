using EventPlatformAPI.DTO;

namespace EventPlatformAPI.Web.Services;

public interface IEventsApiClient
{
    Task<List<EventSummaryDto>> GetEventsAsync();
    Task<EventDetailsDto?> GetEventByIdAsync(int id);
    Task<bool> CreateEventAsync(EventCreateRequestDto request);
    Task<bool> UpdateEventAsync(int id, EventUpdateRequestDto request);
    Task<bool> DeleteEventAsync(int id);

    Task<List<EventTypeDto>> GetEventTypesAsync();
    Task<EventTypeDto?> GetEventTypeByIdAsync(int id);
    Task<bool> CreateEventTypeAsync(EventTypeDto request);
    Task<bool> UpdateEventTypeAsync(int id, EventTypeDto request);
    Task<bool> DeleteEventTypeAsync(int id);

    Task<List<EventLecturerDto>> GetEventLecturersAsync();
    Task<EventLecturerDto?> GetEventLecturerByIdAsync(int id);
    Task<bool> CreateEventLecturerAsync(EventLecturerCreateRequestDto request);
    Task<bool> UpdateEventLecturerAsync(int id, EventLecturerUpdateRequestDto request);
    Task<bool> DeleteEventLecturerAsync(int id);
}
