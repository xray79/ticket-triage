namespace Identity.Contracts;

/// <summary>Anchor type so other modules/tests can reference this assembly without pulling in
/// Identity.Application or Identity.Infrastructure. Currently empty: Identity has no cross-module
/// published events yet in Stage 0 — ICurrentUserAccessor (Shared.Abstractions) covers the one
/// thing other modules need from Identity.</summary>
public static class AssemblyMarker
{
}
