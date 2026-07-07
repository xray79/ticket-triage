using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Shared.Abstractions;

namespace Identity.Infrastructure;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }
    }

    public string Email => Principal?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email) ?? string.Empty;

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll("role").Select(c => c.Value).ToList() ?? new List<string>();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
