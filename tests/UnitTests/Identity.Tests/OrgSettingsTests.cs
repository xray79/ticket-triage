using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Application.OrgSettings;
using NSubstitute;
using Xunit;

namespace Identity.Tests;

public sealed class OrgSettingsTests
{
    [Fact]
    public async Task GetOrgSettingsQuery_returns_the_repository_value()
    {
        var repository = Substitute.For<IOrgSettingsRepository>();
        repository.GetAsync(Arg.Any<CancellationToken>()).Returns(new OrgSettingsDto(ForceLocalOnly: true));
        var handler = new GetOrgSettingsQueryHandler(repository);

        var result = await handler.Handle(new GetOrgSettingsQuery(), CancellationToken.None);

        result.ForceLocalOnly.Should().BeTrue();
    }

    [Fact]
    public async Task SetForceLocalOnlyCommand_persists_the_new_value()
    {
        var repository = Substitute.For<IOrgSettingsRepository>();
        var handler = new SetForceLocalOnlyCommandHandler(repository);

        var result = await handler.Handle(new SetForceLocalOnlyCommand(true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).SetForceLocalOnlyAsync(true, Arg.Any<CancellationToken>());
    }
}
