using FluentAssertions;
using Identity.Domain;
using Xunit;

namespace Identity.Tests;

public sealed class PermissionsTests
{
    [Theory]
    [InlineData(Roles.Agent)]
    [InlineData(Roles.Admin)]
    public void StageZero_roles_have_a_permission_mapping(string role)
    {
        Permissions.ByRole.Should().ContainKey(role);
        Permissions.ByRole[role].Should().NotBeEmpty();
    }

    [Fact]
    public void Only_Admin_can_manage_users()
    {
        Permissions.ByRole[Roles.Agent].Should().NotContain(Permissions.ManageUsers);
        Permissions.ByRole[Roles.Admin].Should().Contain(Permissions.ManageUsers);
    }
}
