namespace Identity.Domain;

/// <summary>Policies map to these, not raw role checks — lets fine-grained permissions grow
/// without touching controllers later.</summary>
public static class Permissions
{
    public const string ViewTickets = "tickets:view";
    public const string TriageTickets = "tickets:triage";
    public const string ResolveTickets = "tickets:resolve";
    public const string ReassignTickets = "tickets:reassign";
    public const string ManageUsers = "users:manage";
    public const string ViewReporting = "reporting:view";
    public const string ManageOrgSettings = "org:manage-settings";

    public static readonly IReadOnlyDictionary<string, string[]> ByRole = new Dictionary<string, string[]>
    {
        [Roles.Agent] = new[] { ViewTickets, TriageTickets, ResolveTickets },
        [Roles.TeamLead] = new[] { ViewTickets, TriageTickets, ResolveTickets, ReassignTickets, ViewReporting },
        [Roles.Admin] = new[] { ViewTickets, TriageTickets, ResolveTickets, ReassignTickets, ManageUsers, ViewReporting, ManageOrgSettings },
        [Roles.Viewer] = new[] { ViewTickets },
    };
}
