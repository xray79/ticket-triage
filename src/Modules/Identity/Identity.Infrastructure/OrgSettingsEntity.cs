namespace Identity.Infrastructure;

/// <summary>Single-row table (fixed Id) holding org-wide policy toggles.</summary>
public sealed class OrgSettingsEntity
{
    public const int SingletonId = 1;

    public int Id { get; init; } = SingletonId;
    public bool ForceLocalOnly { get; set; }
}
