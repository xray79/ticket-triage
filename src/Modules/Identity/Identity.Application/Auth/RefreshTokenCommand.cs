using MediatR;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResultDto>>;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IUserAccountService _userAccountService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;

    public RefreshTokenCommandHandler(
        IUserAccountService userAccountService, ITokenService tokenService, IRefreshTokenStore refreshTokenStore)
    {
        _userAccountService = userAccountService;
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        var newExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(14);

        var userId = await _refreshTokenStore.ValidateAndRotateAsync(
            request.RefreshToken, newRefreshToken, newExpiresAtUtc, cancellationToken);

        if (userId is null)
            return Result.Failure<AuthResultDto>(Error.Validation("Auth.InvalidRefreshToken", "Refresh token is invalid, expired, or revoked."));

        var user = await _userAccountService.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return Result.Failure<AuthResultDto>(Error.NotFound("Auth.UserNotFound", "User no longer exists."));

        var (accessToken, accessExpiresAtUtc) = _tokenService.GenerateAccessToken(user);

        return new AuthResultDto(accessToken, accessExpiresAtUtc, newRefreshToken, user.Id, user.Email, user.DisplayName, user.Roles);
    }
}
