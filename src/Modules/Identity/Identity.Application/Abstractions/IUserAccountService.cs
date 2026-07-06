namespace Identity.Application.Abstractions;

public sealed record UserAccountDto(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

public interface IUserAccountService
{
    Task<UserAccountDto?> ValidateCredentialsAsync(string email, string password, CancellationToken ct);
    Task<UserAccountDto?> GetByIdAsync(Guid userId, CancellationToken ct);
}

public interface IUserRegistrationService
{
    Task<Shared.Kernel.Result<Guid>> CreateAsync(string email, string password, string displayName, string role, CancellationToken ct);
}

public interface IUserPreferenceService
{
    Task<Shared.Kernel.Result<string>> GetProviderPreferenceAsync(Guid userId, CancellationToken ct);
    Task<Shared.Kernel.Result> SetProviderPreferenceAsync(Guid userId, string providerPreference, CancellationToken ct);
}

public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(UserAccountDto user);
    string GenerateRefreshToken();
}

public interface IRefreshTokenStore
{
    Task StoreAsync(Guid userId, string refreshToken, DateTimeOffset expiresAtUtc, CancellationToken ct);

    /// <summary>Returns the user id if the token is valid and unrevoked, and rotates it (revokes the old one).</summary>
    Task<Guid?> ValidateAndRotateAsync(string refreshToken, string newRefreshToken, DateTimeOffset newExpiresAtUtc, CancellationToken ct);

    Task RevokeAsync(string refreshToken, CancellationToken ct);
}
