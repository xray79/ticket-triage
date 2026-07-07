namespace Identity.Application.Auth;

public sealed record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    Guid UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);
