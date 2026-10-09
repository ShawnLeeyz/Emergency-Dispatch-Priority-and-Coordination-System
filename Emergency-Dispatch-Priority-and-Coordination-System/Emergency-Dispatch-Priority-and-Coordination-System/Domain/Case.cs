namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public enum CaseStatus { Open, InProgress, Closed }
public enum Priority { Low, Medium, High }
public enum Severity { Low, Medium, High }
public enum ResponseUnitType { Medical, Police, Fire }

/// <summary>Records which unit responded, its distance, and when it signed off.</summary>
public sealed class CaseAssignment
{
    internal CaseAssignment(Unit unit, DateTimeOffset assignedAt, double distanceKilometres)
        => (Unit, AssignedAt, DistanceKilometres) = (unit, assignedAt, distanceKilometres);

    public Unit Unit { get; }
    public DateTimeOffset AssignedAt { get; }
    public double DistanceKilometres { get; }
    public DateTimeOffset? SignedOffAt { get; private set; }
    public bool IsActive => SignedOffAt is null;

    internal void SignOff(DateTimeOffset signedOffAt) => SignedOffAt = signedOffAt;
}

/// <summary>Aggregate root for an emergency incident. State changes are kept here so they cannot drift across screens.</summary>
public sealed class Case
{
    private readonly List<CaseAssignment> _assignments = [];

    public Guid Id { get; }
    public string CaseNumber => $"CASE-{Id.ToString()[..8].ToUpperInvariant()}";
    public string CallerName { get; }
    public string CallerPhone { get; }
    public string IncidentType { get; }
    public string Description { get; }
    public string Location { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public DateTimeOffset RecordedAt { get; }
    public Severity Severity { get; }
    public Priority CalculatedPriority { get; private set; }
    public Priority Priority { get; private set; }
    public CaseStatus Status { get; private set; } = CaseStatus.Open;
    public IReadOnlyCollection<ResponseUnitType> RequiredUnitTypes { get; }
    public IReadOnlyCollection<CaseAssignment> Assignments => _assignments.AsReadOnly();
    public IReadOnlyCollection<Unit> AssignedUnits => _assignments.Where(a => a.IsActive).Select(a => a.Unit).ToArray();
    public IReadOnlyCollection<ResponseUnitType> WaitingUnitTypes => RequiredUnitTypes.Where(IsWaitingFor).ToArray();

    public Case(string callerName, string callerPhone, string incidentType, string description,
        string location, Severity severity, IEnumerable<ResponseUnitType> requiredUnitTypes,
        DateTimeOffset? recordedAt = null, double latitude = -36.8485, double longitude = 174.7633)
        : this(Guid.NewGuid(), callerName, callerPhone, incidentType, description, location, severity,
            requiredUnitTypes, recordedAt ?? DateTimeOffset.UtcNow, Priority.Low, Priority.Low, CaseStatus.Open,
            latitude, longitude)
    {
    }

    internal Case(Guid id, string callerName, string callerPhone, string incidentType, string description,
        string location, Severity severity, IEnumerable<ResponseUnitType> requiredUnitTypes,
        DateTimeOffset recordedAt, Priority calculatedPriority, Priority priority, CaseStatus status,
        double latitude, double longitude)
    {
        Id = id;
        CallerName = Require(callerName, nameof(callerName));
        CallerPhone = Require(callerPhone, nameof(callerPhone));
        IncidentType = Require(incidentType, nameof(incidentType));
        Description = Require(description, nameof(description));
        Location = Require(location, nameof(location));
        ValidateCoordinates(latitude, longitude);
        Latitude = latitude;
        Longitude = longitude;
        Severity = severity;
        RequiredUnitTypes = requiredUnitTypes?.Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(requiredUnitTypes));
        if (RequiredUnitTypes.Count == 0) throw new ArgumentException("At least one response type is required.", nameof(requiredUnitTypes));
        RecordedAt = recordedAt;
        CalculatedPriority = calculatedPriority;
        Priority = priority;
        Status = status;
    }

    internal void RestoreAssignment(Unit unit, DateTimeOffset assignedAt, DateTimeOffset? signedOffAt,
        double distanceKilometres)
    {
        // Database loading rebuilds the same assignment history without running a new dispatch.
        var assignment = new CaseAssignment(unit, assignedAt, distanceKilometres);
        if (signedOffAt.HasValue) assignment.SignOff(signedOffAt.Value);
        _assignments.Add(assignment);
    }

    public void SetCalculatedPriority(Priority priority)
    {
        // A new case starts with the calculated result as its visible priority.
        CalculatedPriority = priority;
        Priority = priority;
    }

    public void OverridePriority(Priority priority) => Priority = priority;

    public bool Assign(Unit unit, double distanceKilometres = 0)
    {
        ArgumentNullException.ThrowIfNull(unit);
        // Reject closed cases, wrong unit types, duplicate departments, and unavailable units.
        if (Status == CaseStatus.Closed || !RequiredUnitTypes.Contains(unit.Type) ||
            _assignments.Any(a => a.Unit.Type == unit.Type) || !unit.TryAssign(this)) return false;
        if (distanceKilometres < 0) throw new ArgumentOutOfRangeException(nameof(distanceKilometres));
        _assignments.Add(new CaseAssignment(unit, DateTimeOffset.UtcNow, distanceKilometres));
        Status = CaseStatus.InProgress;
        return true;
    }

    public bool SignOff(Guid unitId)
    {
        // Only an active assignment can sign off and release its response unit.
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
        // Any active unit keeps the case in progress.
        if (_assignments.Any(a => a.IsActive))
        {
            Status = CaseStatus.InProgress;
            return;
        }

        // The case closes only when every required department has completed a response.
        Status = RequiredUnitTypes.All(type =>
            _assignments.Any(a => a.Unit.Type == type && a.SignedOffAt.HasValue))
            ? CaseStatus.Closed
            : CaseStatus.Open;
    }

    private static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("This field is required.", name) : value.Trim();

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
    }
}
