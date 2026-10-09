namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

/// <summary>Lists the important actions that can appear in the audit log.</summary>
public static class AuditEventTypes
{
    public const string CaseCreated = "Case created";
    public const string UnitAssigned = "Unit assigned";
    public const string UnitUpdated = "Unit updated";
    public const string UnitSignedOff = "Unit signed off";
    public const string CaseClosed = "Case closed";
    public const string PriorityOverridden = "Priority overridden";
}

/// <summary>An append-only record of an important action performed in the system.</summary>
public sealed record AuditEvent(
    Guid Id,
    DateTimeOffset CreatedAt,
    string EventType,
    string PerformedBy,
    Guid? CaseId,
    string? CaseNumber,
    string? UnitIdentifier,
    string? OldValue,
    string? NewValue,
    string? Reason);
