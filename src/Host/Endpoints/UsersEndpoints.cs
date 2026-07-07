using MediatR;
using Identity.Application.Users;
using Identity.Domain;

namespace Host.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization(Permissions.ManageUsers);

        group.MapPost("/", async (RegisterUserRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterUserCommand(request.Email, request.Password, request.DisplayName, request.Role), ct);
            return result.IsSuccess ? Results.Created($"/api/users/{result.Value}", new { id = result.Value }) : Results.BadRequest(result.Error.Message);
        });
    }

    public sealed record RegisterUserRequest(string Email, string Password, string DisplayName, string Role);
}
