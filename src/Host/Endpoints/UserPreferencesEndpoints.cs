using MediatR;
using Identity.Application.Preferences;
using Shared.Abstractions;

namespace Host.Endpoints;

public static class UserPreferencesEndpoints
{
    public static void MapUserPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me/provider-preference").WithTags("Users").RequireAuthorization();

        group.MapGet("/", async (ISender sender, ICurrentUserAccessor currentUser, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProviderPreferenceQuery(currentUser.UserId), ct);
            return result.IsSuccess ? Results.Ok(new ProviderPreferenceResponse(result.Value)) : Results.NotFound(result.Error.Message);
        }).Produces<ProviderPreferenceResponse>().Produces<string>(404);

        group.MapPut("/", async (SetProviderPreferenceRequest request, ISender sender, ICurrentUserAccessor currentUser, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetProviderPreferenceCommand(currentUser.UserId, request.ProviderPreference), ct);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error.Message);
        }).Produces(204).Produces<string>(400);
    }

    public sealed record SetProviderPreferenceRequest(string ProviderPreference);
    public sealed record ProviderPreferenceResponse(string ProviderPreference);
}
