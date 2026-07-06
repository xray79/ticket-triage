using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Per-user opt-in preference: "local" (default, private) or a cloud provider key.</summary>
    public string ProviderPreference { get; set; } = "local";
}
