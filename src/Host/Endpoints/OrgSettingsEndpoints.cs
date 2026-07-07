using MediatR;
using Identity.Application.Abstractions;
using Identity.Application.OrgSettings;
using Identity.Domain;

namespace Host.Endpoints;

public static class OrgSettingsEndpoints
{
    public static void MapOrgSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/org-settings").WithTags("Admin").RequireAuthorization(Permissions.ManageOrgSettings);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var settings = await sender.Send(new GetOrgSettingsQuery(), ct);
            return Results.Ok(settings);
        }).Produces<OrgSettingsDto>();

        group.MapPut("/", async (SetOrgSettingsRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetForceLocalOnlyCommand(request.ForceLocalOnly), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error.Message);
        }).Produces(204).Produces<string>(400);
    }

    public sealed record SetOrgSettingsRequest(bool ForceLocalOnly);
}
