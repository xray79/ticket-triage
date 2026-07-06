using MediatR;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Application.Auth;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUserAccountService _userAccountService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;

    public LoginCommandHandler(
        IUserAccountService userAccountService, ITokenService tokenService, IRefreshTokenStore refreshTokenStore)
    {
        _userAccountService = userAccountService;
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userAccountService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
            return Result.Failure<AuthResultDto>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        var (accessToken, expiresAtUtc) = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokenStore.StoreAsync(user.Id, refreshToken, DateTimeOffset.UtcNow.AddDays(14), cancellationToken);

        return new AuthResultDto(accessToken, expiresAtUtc, refreshToken, user.Id, user.Email, user.DisplayName, user.Roles);
    }
}
