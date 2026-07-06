using MediatR;
using Microsoft.AspNetCore.Mvc;
using Identity.Application.Auth;

namespace Host.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message, statusCode: 401);
        });

        group.MapPost("/refresh", async (RefreshRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(request.RefreshToken), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error.Message, statusCode: 401);
        });
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
}
