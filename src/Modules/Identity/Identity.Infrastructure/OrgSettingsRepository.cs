using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;

namespace Identity.Infrastructure;

public sealed class OrgSettingsRepository : IOrgSettingsRepository
{
    private readonly IdentityDbContext _context;

    public OrgSettingsRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<OrgSettingsDto> GetAsync(CancellationToken ct)
    {
        var entity = await GetOrCreateAsync(ct);
        return new OrgSettingsDto(entity.ForceLocalOnly);
    }

    public async Task SetForceLocalOnlyAsync(bool forceLocalOnly, CancellationToken ct)
    {
        var entity = await GetOrCreateAsync(ct);
        entity.ForceLocalOnly = forceLocalOnly;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<OrgSettingsEntity> GetOrCreateAsync(CancellationToken ct)
    {
        var entity = await _context.OrgSettings.FirstOrDefaultAsync(x => x.Id == OrgSettingsEntity.SingletonId, ct);
        if (entity is not null)
            return entity;

        entity = new OrgSettingsEntity();
        _context.OrgSettings.Add(entity);
        await _context.SaveChangesAsync(ct);
        return entity;
    }
}
