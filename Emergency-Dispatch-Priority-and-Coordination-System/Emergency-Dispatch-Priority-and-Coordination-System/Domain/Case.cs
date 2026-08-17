namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public enum CaseStatus { Open, InProgress, Closed }
public enum Priority { Low, Medium, High }
public enum Severity { Low, Medium, High }
public enum ResponseUnitType { Medical, Police, Fire }

/// <summary>Aggregate root for an emergency incident. State changes are kept here so they cannot drift across screens.</summary>
public sealed class Case
{
    private readonly List<Unit> _assignedUnits = [];

    public Guid Id { get; } = Guid.NewGuid();
    public string CaseNumber => $"CASE-{Id.ToString()[..8].ToUpperInvariant()}";
    public string CallerName { get; }
    public string CallerPhone { get; }
    public string IncidentType { get; }
    public string Description { get; }
    public string Location { get; }
    public DateTimeOffset RecordedAt { get; }
    public Severity Severity { get; }
    public Priority Priority { get; private set; }
    public CaseStatus Status { get; private set; } = CaseStatus.Open;
    public IReadOnlyCollection<ResponseUnitType> RequiredUnitTypes { get; }
    public IReadOnlyCollection<Unit> AssignedUnits => _assignedUnits.AsReadOnly();

    public Case(string callerName, string callerPhone, string incidentType, string description,
        string location, Severity severity, IEnumerable<ResponseUnitType> requiredUnitTypes,
        DateTimeOffset? recordedAt = null)
    {
        CallerName = Require(callerName, nameof(callerName));
        CallerPhone = Require(callerPhone, nameof(callerPhone));
        IncidentType = Require(incidentType, nameof(incidentType));
        Description = Require(description, nameof(description));
        Location = Require(location, nameof(location));
        Severity = severity;
        RequiredUnitTypes = requiredUnitTypes?.Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(requiredUnitTypes));
        if (RequiredUnitTypes.Count == 0) throw new ArgumentException("At least one response type is required.", nameof(requiredUnitTypes));
        RecordedAt = recordedAt ?? DateTimeOffset.UtcNow;
    }

    public void SetPriority(Priority priority) => Priority = priority;

    public bool Assign(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        if (Status == CaseStatus.Closed || _assignedUnits.Contains(unit) || !unit.TryAssign(this)) return false;
        _assignedUnits.Add(unit);
        Status = CaseStatus.InProgress;
        return true;
    }

    public void Close()
    {
        if (Status == CaseStatus.Closed) return;
        foreach (var unit in _assignedUnits) unit.Release();
        _assignedUnits.Clear();
        Status = CaseStatus.Closed;
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("This field is required.", name) : value.Trim();
}
