using MediatR;
using Shared.Kernel;

namespace Identity.Application.Users;

public sealed record RegisterUserCommand(string Email, string Password, string DisplayName, string Role) : IRequest<Result<Guid>>;
