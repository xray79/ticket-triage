using MediatR;
using Identity.Application.OrgSettings;
using Identity.Application.Preferences;
using Identity.Domain;
using Shared.Abstractions;
using Tickets.Application.Tickets.AssignTicket;
using Tickets.Application.Tickets.CreateTicket;
using Tickets.Application.Tickets.GetTicket;
using Tickets.Application.Tickets.ListTickets;
using Tickets.Application.Tickets.ResolveTicket;
using Tickets.Domain;

namespace Host.Endpoints;

public static class TicketsEndpoints
{
    public static void MapTicketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").WithTags("Tickets").RequireAuthorization();

        group.MapPost("/", async (CreateTicketRequest request, ISender sender, ICurrentUserAccessor currentUser, CancellationToken ct) =>
        {
            var effectiveProvider = await ResolveEffectiveProviderAsync(request.RequestedProvider, sender, currentUser, ct);
            var command = new CreateTicketCommand(
                request.Subject, request.Body, request.CustomerEmail, effectiveProvider, currentUser.UserId);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.Created($"/api/tickets/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error.Message);
        }).RequireAuthorization(Permissions.TriageTickets);

        group.MapGet("/", async (ISender sender, CancellationToken ct, string? status) =>
        {
            TicketStatus? parsedStatus = status is not null && Enum.TryParse<TicketStatus>(status, true, out var s) ? s : null;
            var result = await sender.Send(new ListTicketsQuery(parsedStatus), ct);
            return Results.Ok(result);
        }).RequireAuthorization(Permissions.ViewTickets);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTicketQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error.Message);
        }).RequireAuthorization(Permissions.ViewTickets);

        group.MapPost("/{id:guid}/resolve", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResolveTicketCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error.Message);
        }).RequireAuthorization(Permissions.ResolveTickets);

        group.MapPost("/{id:guid}/assign", async (Guid id, AssignTicketRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AssignTicketCommand(id, request.AssigneeUserId), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error.Message);
        }).RequireAuthorization(Permissions.ReassignTickets);
    }

    /// <summary>
    /// Org policy (force-local-only) always wins; otherwise an explicit per-ticket choice from
    /// the request wins; otherwise the agent's saved preference; otherwise "local". Resolved here
    /// in the Host composition root rather than in Tickets or Triage, since it needs both an
    /// Identity user preference and an Identity org setting — neither module may reach into
    /// Identity's data directly (see the module-boundary rule and its NetArchTest enforcement).
    /// </summary>
    private static async Task<string> ResolveEffectiveProviderAsync(
        string? requestedProvider, ISender sender, ICurrentUserAccessor currentUser, CancellationToken ct)
    {
        var orgSettings = await sender.Send(new GetOrgSettingsQuery(), ct);
        if (orgSettings.ForceLocalOnly)
            return "local";

        if (!string.IsNullOrWhiteSpace(requestedProvider))
            return requestedProvider;

        var preference = await sender.Send(new GetProviderPreferenceQuery(currentUser.UserId), ct);
        return preference.IsSuccess ? preference.Value : "local";
    }

    public sealed record CreateTicketRequest(string Subject, string Body, string CustomerEmail, string? RequestedProvider);
    public sealed record AssignTicketRequest(Guid AssigneeUserId);
}
