namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public enum CaseStatus { Open, InProgress, Closed }
public enum Priority { Low, Medium, High }
public enum Severity { Low, Medium, High }
public enum ResponseUnitType { Medical, Police, Fire }

public sealed class CaseAssignment
{
    internal CaseAssignment(Unit unit, DateTimeOffset assignedAt)
        => (Unit, AssignedAt) = (unit, assignedAt);

    public Unit Unit { get; }
    public DateTimeOffset AssignedAt { get; }
    public DateTimeOffset? SignedOffAt { get; private set; }
    public bool IsActive => SignedOffAt is null;

    internal void SignOff(DateTimeOffset signedOffAt) => SignedOffAt = signedOffAt;
}

/// <summary>Aggregate root for an emergency incident. State changes are kept here so they cannot drift across screens.</summary>
public sealed class Case
{
    private readonly List<CaseAssignment> _assignments = [];

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
    public IReadOnlyCollection<CaseAssignment> Assignments => _assignments.AsReadOnly();
    public IReadOnlyCollection<Unit> AssignedUnits => _assignments.Where(a => a.IsActive).Select(a => a.Unit).ToArray();
    public IReadOnlyCollection<ResponseUnitType> WaitingUnitTypes => RequiredUnitTypes.Where(IsWaitingFor).ToArray();

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
        if (Status == CaseStatus.Closed || !RequiredUnitTypes.Contains(unit.Type) ||
            _assignments.Any(a => a.Unit.Type == unit.Type) || !unit.TryAssign(this)) return false;
        _assignments.Add(new CaseAssignment(unit, DateTimeOffset.UtcNow));
        Status = CaseStatus.InProgress;
        return true;
    }

    public bool SignOff(Guid unitId)
    {
        var assignment = _assignments.SingleOrDefault(a => a.IsActive && a.Unit.Id == unitId);
        if (assignment is null) return false;

        assignment.SignOff(DateTimeOffset.UtcNow);
        assignment.Unit.Release();
        UpdateStatus();
        return true;
    }

    public bool Unassign(Guid unitId)
    {
        var assignment = _assignments.SingleOrDefault(a => a.IsActive && a.Unit.Id == unitId);
        if (assignment is null) return false;

        assignment.Unit.Release();
        _assignments.Remove(assignment);
        UpdateStatus();
        return true;
    }

    public bool IsWaitingFor(ResponseUnitType type) =>
        Status != CaseStatus.Closed && RequiredUnitTypes.Contains(type) &&
        !_assignments.Any(a => a.Unit.Type == type);

    private void UpdateStatus()
    {
        if (_assignments.Any(a => a.IsActive))
        {
            Status = CaseStatus.InProgress;
            return;
        }

        Status = RequiredUnitTypes.All(type =>
            _assignments.Any(a => a.Unit.Type == type && a.SignedOffAt.HasValue))
            ? CaseStatus.Closed
            : CaseStatus.Open;
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("This field is required.", name) : value.Trim();
}
