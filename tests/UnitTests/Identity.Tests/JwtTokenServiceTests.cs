using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.Tests;

public sealed class JwtTokenServiceTests
{
    private static JwtTokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = Convert.ToBase64String(new byte[32]),
        AccessTokenMinutes = 15
    }));

    [Fact]
    public void GenerateAccessToken_embeds_permission_claims_derived_from_roles()
    {
        var service = CreateService();
        var user = new UserAccountDto(Guid.NewGuid(), "agent@example.com", "Agent Smith", new[] { "Agent" });

        var (token, _) = service.GenerateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "tickets:triage");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        // Must stay the short "role" claim type: the Angular client decodes the JWT payload
        // directly and RoleClaimType is configured to match on the validation side too.
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Agent");
    }

    [Fact]
    public void GenerateRefreshToken_returns_a_unique_value_each_call()
    {
        var service = CreateService();

        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        first.Should().NotBe(second);
    }
}
