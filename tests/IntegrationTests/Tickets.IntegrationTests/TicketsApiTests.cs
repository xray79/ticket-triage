using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Tickets.IntegrationTests;

/// <summary>
/// Exercises the real HTTP pipeline (auth, MediatR, EF Core, outbox) against a throwaway
/// Postgres container — not runnable in this sandboxed session (Docker image pulls are
/// policy-blocked here), but wired to run in any environment where `docker pull` works,
/// including CI.
/// </summary>
public sealed class TicketsApiTests : IClassFixture<TicketTriageApiFactory>
{
    private readonly TicketTriageApiFactory _factory;

    public TicketsApiTests(TicketTriageApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var email = $"agent-{Guid.NewGuid():N}@example.com";
        const string password = "AgentPass123!";
        await _factory.CreateAgentAndLoginAsync(email, password);

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Creating_a_ticket_then_fetching_it_round_trips_through_the_real_pipeline()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            subject = "Integration test ticket",
            body = "Body text",
            customerEmail = "customer@example.com",
            requestedProvider = "local"
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var getResponse = await client.GetAsync($"/api/tickets/{created!.Id}");
        getResponse.EnsureSuccessStatusCode();
        var ticket = await getResponse.Content.ReadFromJsonAsync<TicketResponse>();

        ticket!.Subject.Should().Be("Integration test ticket");
        ticket.Status.Should().Be("New");
        ticket.Triage.Should().BeNull();
    }

    [Fact]
    public async Task Listing_tickets_requires_authentication()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/tickets");

        ((int)response.StatusCode).Should().Be(401);
    }

    private sealed record LoginResponse(string AccessToken);
    private sealed record CreatedResponse(Guid Id);
    private sealed record TicketResponse(Guid Id, string Subject, string Status, object? Triage);
}
