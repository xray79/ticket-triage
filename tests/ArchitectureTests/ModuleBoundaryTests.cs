using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace ArchitectureTests;

/// <summary>
/// Enforces the module map's core rule: modules only talk to each other through the other
/// module's Contracts project. A reference from one module's Domain/Application/Infrastructure
/// straight into another module's Domain/Application/Infrastructure is a boundary violation and
/// should fail CI, not just code review.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly (string Name, Assembly Domain, Assembly Application, Assembly Infrastructure, Assembly Contracts)[] Modules =
    {
        ("Tickets", typeof(Tickets.Domain.Ticket).Assembly, typeof(Tickets.Application.DependencyInjection).Assembly,
            typeof(Tickets.Infrastructure.TicketsDbContext).Assembly, typeof(Tickets.Contracts.Events.TicketCreated).Assembly),
        ("Triage", typeof(Triage.Domain.TriageRecord).Assembly, typeof(Triage.Application.TicketContent).Assembly,
            typeof(Triage.Infrastructure.TriageDbContext).Assembly, typeof(Triage.Contracts.Events.TicketTriaged).Assembly),
        ("Identity", typeof(Identity.Domain.Roles).Assembly, typeof(Identity.Application.DependencyInjection).Assembly,
            typeof(Identity.Infrastructure.ApplicationUser).Assembly, typeof(Identity.Contracts.AssemblyMarker).Assembly),
        ("Notifications", typeof(Notifications.Domain.NotificationLog).Assembly, typeof(Notifications.Application.DependencyInjection).Assembly,
            typeof(Notifications.Infrastructure.NotificationsDbContext).Assembly, typeof(Notifications.Contracts.AssemblyMarker).Assembly),
        ("Reporting", typeof(Reporting.Domain.TicketReportEntry).Assembly, typeof(Reporting.Application.DependencyInjection).Assembly,
            typeof(Reporting.Infrastructure.ReportingDbContext).Assembly, typeof(Reporting.Contracts.AssemblyMarker).Assembly),
    };

    public static IEnumerable<object[]> ModuleLayerPairsAcrossModules()
    {
        foreach (var owner in Modules)
        {
            foreach (var other in Modules)
            {
                if (owner.Name == other.Name)
                    continue;

                yield return new object[] { owner.Name, owner.Domain, other };
                yield return new object[] { owner.Name, owner.Application, other };
                yield return new object[] { owner.Name, owner.Infrastructure, other };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ModuleLayerPairsAcrossModules))]
    public void Module_layers_must_not_reference_another_modules_internals(
        string ownerModuleName, Assembly ownerLayerAssembly, (string Name, Assembly Domain, Assembly Application, Assembly Infrastructure, Assembly Contracts) otherModule)
    {
        var result = Types.InAssembly(ownerLayerAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                otherModule.Domain.GetName().Name!,
                otherModule.Application.GetName().Name!,
                otherModule.Infrastructure.GetName().Name!)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{ownerModuleName} ({ownerLayerAssembly.GetName().Name}) must only reference {otherModule.Name} via " +
            $"{otherModule.Contracts.GetName().Name}, not its Domain/Application/Infrastructure. Violations: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Domain_layers_must_not_depend_on_persistence_or_mediator_frameworks()
    {
        foreach (var module in Modules)
        {
            var result = Types.InAssembly(module.Domain)
                .Should()
                .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "Microsoft.AspNetCore.Identity")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module.Name}.Domain must stay framework-free. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }
    }

    [Fact]
    public void Application_layers_must_not_depend_on_infrastructure_frameworks()
    {
        foreach (var module in Modules)
        {
            var result = Types.InAssembly(module.Application)
                .Should()
                .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "AWSSDK.SQS", "Microsoft.AspNetCore.Identity")
                .GetResult();

            Assert.True(result.IsSuccessful,
                $"{module.Name}.Application must stay persistence/transport-ignorant. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
        }
    }
}
