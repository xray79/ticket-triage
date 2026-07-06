namespace Identity.Domain;

/// <summary>Stage 0 wires Agent + Admin only; TeamLead/Viewer are reserved for a later add-on.</summary>
public static class Roles
{
    public const string Agent = "Agent";
    public const string TeamLead = "TeamLead";
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> StageZero = new[] { Agent, Admin };
}
