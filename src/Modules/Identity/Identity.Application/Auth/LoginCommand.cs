using MediatR;
using Shared.Kernel;

namespace Identity.Application.Auth;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResultDto>>;
