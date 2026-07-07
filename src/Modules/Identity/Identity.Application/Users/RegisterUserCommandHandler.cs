using MediatR;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Application.Users;

public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IUserRegistrationService _registrationService;

    public RegisterUserCommandHandler(IUserRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken) =>
        _registrationService.CreateAsync(request.Email, request.Password, request.DisplayName, request.Role, cancellationToken);
}
