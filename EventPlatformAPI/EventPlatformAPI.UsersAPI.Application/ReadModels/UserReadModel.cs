namespace EventPlatformAPI.UsersAPI.Application.ReadModels
{
    public record UserReadModel(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    int RegistrationsCount);
}
