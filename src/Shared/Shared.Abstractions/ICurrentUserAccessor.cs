namespace Shared.Abstractions;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    string Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
