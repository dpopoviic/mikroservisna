namespace EventPlatformAPI.UsersAPI.Application.ReadModels
{
    public record RegistrationRequest(
    Guid Id,
    Guid UserId,
    Guid EventId,
    string Status,
    DateTime CreatedAt,
    string UserFirstName,
    string UserLastName,
    string UserEmail);
}
