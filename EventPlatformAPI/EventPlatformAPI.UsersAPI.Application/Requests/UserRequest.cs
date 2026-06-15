namespace EventPlatformAPI.UsersAPI.Application.Requests
{
    public record UserRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    int RegistrationsCount);
}
