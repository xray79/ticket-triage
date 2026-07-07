using Microsoft.AspNetCore.Identity;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Infrastructure;

public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRegistrationService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> CreateAsync(string email, string password, string displayName, string role, CancellationToken ct)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var message = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(Error.Validation("User.CreateFailed", message));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var message = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result.Failure<Guid>(Error.Validation("User.RoleAssignmentFailed", message));
        }

        return user.Id;
    }
}
