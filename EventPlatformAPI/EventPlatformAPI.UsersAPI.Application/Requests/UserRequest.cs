namespace EventPlatformAPI.UsersAPI.Application.ReadModels
{
    public record UserRequest(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    int RegistrationsCount);
}
