using Identity.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Tickets.Infrastructure;
using Triage.Infrastructure;

namespace Tickets.IntegrationTests;

/// <summary>
/// Boots the real Host against a throwaway Postgres container instead of mocks, so these
/// tests exercise the actual DI wiring, EF mappings, and outbox behavior. SQS is left
/// pointed at an unreachable endpoint — the outbox dispatcher's per-message try/catch
/// (see Shared.Infrastructure.Outbox.OutboxDispatcherHostedService) means that degrades
/// to logged errors rather than failing the app, matching production behavior when the
/// queue is briefly unavailable.
/// </summary>
public sealed class TicketTriageApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("ticket_triage")
        .WithUsername("ticket_triage")
        .WithPassword("ticket_triage")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Tickets"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Triage"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Identity"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Notifications"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Reporting"] = _postgres.GetConnectionString(),
                ["Jwt:SigningKey"] = Convert.ToBase64String(new byte[32]),
                ["Sqs:ServiceUrl"] = "http://127.0.0.1:1", // deliberately unreachable
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task<string> CreateAgentAndLoginAsync(string email, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole<Guid>>>();

        if (!await roleManager.RoleExistsAsync(Identity.Domain.Roles.Agent))
            await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole<Guid>(Identity.Domain.Roles.Agent));

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = "Test Agent" };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, Identity.Domain.Roles.Agent);

        return email;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
    }
}
