using FluentAssertions;
using Identity.Application.Abstractions;
using Identity.Application.Preferences;
using NSubstitute;
using Shared.Kernel;
using Xunit;

namespace Identity.Tests;

public sealed class ProviderPreferenceTests
{
    [Theory]
    [InlineData("local")]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("gemini")]
    public async Task SetProviderPreferenceCommand_accepts_every_known_provider(string provider)
    {
        var service = Substitute.For<IUserPreferenceService>();
        service.SetProviderPreferenceAsync(Arg.Any<Guid>(), provider, Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new SetProviderPreferenceCommandHandler(service);

        var result = await handler.Handle(new SetProviderPreferenceCommand(Guid.NewGuid(), provider), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetProviderPreferenceCommand_rejects_an_unrecognized_provider_without_calling_the_service()
    {
        var service = Substitute.For<IUserPreferenceService>();
        var handler = new SetProviderPreferenceCommandHandler(service);

        var result = await handler.Handle(new SetProviderPreferenceCommand(Guid.NewGuid(), "made-up-provider"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await service.DidNotReceive().SetProviderPreferenceAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProviderPreferenceQuery_delegates_to_the_service()
    {
        var userId = Guid.NewGuid();
        var service = Substitute.For<IUserPreferenceService>();
        service.GetProviderPreferenceAsync(userId, Arg.Any<CancellationToken>()).Returns(Result.Success("openai"));
        var handler = new GetProviderPreferenceQueryHandler(service);

        var result = await handler.Handle(new GetProviderPreferenceQuery(userId), CancellationToken.None);

        result.Value.Should().Be("openai");
    }
}
