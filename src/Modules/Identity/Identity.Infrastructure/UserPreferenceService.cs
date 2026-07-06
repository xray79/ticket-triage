using Microsoft.AspNetCore.Identity;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Infrastructure;

public sealed class UserPreferenceService : IUserPreferenceService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserPreferenceService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<string>> GetProviderPreferenceAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<string>(Error.NotFound("User.NotFound", "User not found."));

        return user.ProviderPreference;
    }

    public async Task<Result> SetProviderPreferenceAsync(Guid userId, string providerPreference, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure(Error.NotFound("User.NotFound", "User not found."));

        user.ProviderPreference = providerPreference;
        await _userManager.UpdateAsync(user);
        return Result.Success();
    }
}
