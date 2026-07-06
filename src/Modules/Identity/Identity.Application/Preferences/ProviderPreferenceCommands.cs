using MediatR;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Application.Preferences;

public static class ProviderPreferences
{
    public static readonly IReadOnlyCollection<string> Valid = new[] { "local", "openai", "anthropic", "gemini" };
}

public sealed record GetProviderPreferenceQuery(Guid UserId) : IRequest<Result<string>>;

public sealed class GetProviderPreferenceQueryHandler : IRequestHandler<GetProviderPreferenceQuery, Result<string>>
{
    private readonly IUserPreferenceService _service;

    public GetProviderPreferenceQueryHandler(IUserPreferenceService service)
    {
        _service = service;
    }

    public Task<Result<string>> Handle(GetProviderPreferenceQuery request, CancellationToken cancellationToken) =>
        _service.GetProviderPreferenceAsync(request.UserId, cancellationToken);
}

public sealed record SetProviderPreferenceCommand(Guid UserId, string ProviderPreference) : IRequest<Result>;

public sealed class SetProviderPreferenceCommandHandler : IRequestHandler<SetProviderPreferenceCommand, Result>
{
    private readonly IUserPreferenceService _service;

    public SetProviderPreferenceCommandHandler(IUserPreferenceService service)
    {
        _service = service;
    }

    public Task<Result> Handle(SetProviderPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (!ProviderPreferences.Valid.Contains(request.ProviderPreference))
        {
            return Task.FromResult(Result.Failure(
                Error.Validation("ProviderPreference.Invalid", $"'{request.ProviderPreference}' is not a recognized provider.")));
        }

        return _service.SetProviderPreferenceAsync(request.UserId, request.ProviderPreference, cancellationToken);
    }
}
