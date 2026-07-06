using MediatR;
using Identity.Application.Abstractions;
using Shared.Kernel;

namespace Identity.Application.OrgSettings;

public sealed record GetOrgSettingsQuery : IRequest<OrgSettingsDto>;

public sealed class GetOrgSettingsQueryHandler : IRequestHandler<GetOrgSettingsQuery, OrgSettingsDto>
{
    private readonly IOrgSettingsRepository _repository;

    public GetOrgSettingsQueryHandler(IOrgSettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<OrgSettingsDto> Handle(GetOrgSettingsQuery request, CancellationToken cancellationToken) =>
        _repository.GetAsync(cancellationToken);
}

/// <summary>Admin-only org-wide policy override — e.g. force every ticket to triage locally
/// regardless of individual user preference, for orgs that can't allow any cloud escalation.</summary>
public sealed record SetForceLocalOnlyCommand(bool ForceLocalOnly) : IRequest<Result>;

public sealed class SetForceLocalOnlyCommandHandler : IRequestHandler<SetForceLocalOnlyCommand, Result>
{
    private readonly IOrgSettingsRepository _repository;

    public SetForceLocalOnlyCommandHandler(IOrgSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SetForceLocalOnlyCommand request, CancellationToken cancellationToken)
    {
        await _repository.SetForceLocalOnlyAsync(request.ForceLocalOnly, cancellationToken);
        return Result.Success();
    }
}
