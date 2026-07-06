using Microsoft.AspNetCore.Identity;
using Identity.Application.Abstractions;

namespace Identity.Infrastructure;

public sealed class UserAccountService : IUserAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccountService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserAccountDto?> ValidateCredentialsAsync(string email, string password, CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return null;

        return await ToDtoAsync(user);
    }

    public async Task<UserAccountDto?> GetByIdAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await ToDtoAsync(user);
    }

    private async Task<UserAccountDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserAccountDto(user.Id, user.Email!, user.DisplayName, roles.ToList());
    }
}
