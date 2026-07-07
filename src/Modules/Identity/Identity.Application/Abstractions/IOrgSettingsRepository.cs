namespace Identity.Application.Abstractions;

public sealed record OrgSettingsDto(bool ForceLocalOnly);

public interface IOrgSettingsRepository
{
    Task<OrgSettingsDto> GetAsync(CancellationToken ct);
    Task SetForceLocalOnlyAsync(bool forceLocalOnly, CancellationToken ct);
}
