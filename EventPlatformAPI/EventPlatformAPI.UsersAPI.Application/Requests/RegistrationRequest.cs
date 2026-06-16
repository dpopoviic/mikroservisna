namespace EventPlatformAPI.UsersAPI.Application.Requests
{
    public record RegistrationRequest(
    Guid Id,
    Guid UserId,
    int EventId,
    string Status,
    DateTime CreatedAt,
    string UserFirstName,
    string UserLastName,
    string UserEmail);
}
