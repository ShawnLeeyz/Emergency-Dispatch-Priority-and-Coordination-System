using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Coordinates the main case, assignment, sign-off, notification, and audit workflows.</summary>
public sealed class DispatchService
{
    private readonly ICaseRepository _cases;
    private readonly IDepartmentRepository _departments;
    private readonly IPriorityStrategy _priorityStrategy;
    private readonly IDispatchNotifier _notifier;
    private readonly IAuditRepository _audit;
    private readonly IUnitAssignmentService _unitAssignment;
    private readonly Lock _dispatchLock = new();

    public DispatchService(ICaseRepository cases, IDepartmentRepository departments, IPriorityStrategy priorityStrategy,
        IDispatchNotifier notifier, IAuditRepository audit, IUnitAssignmentService unitAssignment)
        => (_cases, _departments, _priorityStrategy, _notifier, _audit, _unitAssignment) =
            (cases, departments, priorityStrategy, notifier, audit, unitAssignment);

    public Case CreateAndDispatch(CreateCaseRequest request, string performedBy = "System")
    {
        ArgumentNullException.ThrowIfNull(request);
        // Build one valid case before it is prioritised, saved, and assigned.
        var dispatchCase = new Case(request.CallerName, request.CallerPhone, request.IncidentType,
            request.Description, request.Location, request.Severity, request.RequiredUnitTypes,
            latitude: request.Latitude, longitude: request.Longitude);
        dispatchCase.SetCalculatedPriority(_priorityStrategy.Calculate(dispatchCase));
        lock (_dispatchLock)
        {
            // The lock keeps two dispatch requests from assigning the same unit at the same time.
            _cases.Add(dispatchCase);
            AddAudit(AuditEventTypes.CaseCreated, performedBy, dispatchCase, null, null,
                $"Priority: {dispatchCase.Priority}; Departments: {string.Join(", ", dispatchCase.RequiredUnitTypes)}",
                "Emergency case submitted.");
            AssignAvailableUnits(dispatchCase, performedBy);
        }
        return dispatchCase;
    }

    public void SignOffUnit(Guid caseId, Guid unitId, ResponseUnitType departmentType,
        string performedBy = "System")
    {
        lock (_dispatchLock)
        {
            var dispatchCase = _cases.Get(caseId) ?? throw new KeyNotFoundException("The selected case no longer exists.");
            var unit = dispatchCase.AssignedUnits.SingleOrDefault(candidate => candidate.Id == unitId)
                ?? throw new InvalidOperationException("That unit is not actively assigned to this case.");
            if (unit.Type != departmentType)
                throw new InvalidOperationException("That unit is managed by a different department.");
            // The domain object releases the unit and decides whether the case can close.
            var previousStatus = dispatchCase.Status;
            dispatchCase.SignOff(unitId);
            AddAudit(AuditEventTypes.UnitSignedOff, performedBy, dispatchCase, unit.Identifier,
                "Assigned", "Available", "Response unit completed its assignment.");
            if (previousStatus != CaseStatus.Closed && dispatchCase.Status == CaseStatus.Closed)
                AddAudit(AuditEventTypes.CaseClosed, performedBy, dispatchCase, null,
                    previousStatus.ToString(), CaseStatus.Closed.ToString(), "All required units signed off.");

            // Reuse the released unit for the oldest compatible waiting case.
            var nextCase = AssignNextWaitingCase(unit, performedBy);
            _cases.Save(dispatchCase);
            if (nextCase is not null) _cases.Save(nextCase);
            _departments.Save(unit);
        }
    }

    public void UpdateUnit(ResponseUnitType departmentType, Guid unitId, string location, int personnelCount,
        string performedBy = "System", double? latitude = null, double? longitude = null)
    {
        lock (_dispatchLock)
        {
            var unit = _departments.Get(departmentType)?.Units.SingleOrDefault(candidate => candidate.Id == unitId)
                ?? throw new KeyNotFoundException("The selected response unit could not be found in that department.");
            var oldValue = $"Location: {unit.Location}; Coordinates: {unit.Latitude:F5}, {unit.Longitude:F5}; Personnel: {unit.PersonnelCount}";
            unit.UpdateDetails(location, personnelCount, latitude ?? unit.Latitude, longitude ?? unit.Longitude);
            _departments.Save(unit);
            AddAudit(AuditEventTypes.UnitUpdated, performedBy, null, unit.Identifier, oldValue,
                $"Location: {unit.Location}; Coordinates: {unit.Latitude:F5}, {unit.Longitude:F5}; Personnel: {unit.PersonnelCount}",
                "Unit details updated.");
        }
    }

    public void OverridePriority(Guid caseId, Priority newPriority, string reason, string performedBy)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required when overriding priority.", nameof(reason));

        lock (_dispatchLock)
        {
            var dispatchCase = _cases.Get(caseId)
                ?? throw new KeyNotFoundException("The selected case no longer exists.");
            if (dispatchCase.Status == CaseStatus.Closed)
                throw new InvalidOperationException("A closed case cannot have its priority overridden.");
            if (dispatchCase.Priority == newPriority)
                throw new ArgumentException("Select a priority that is different from the current priority.", nameof(newPriority));

            // Keep the calculated priority unchanged so both decisions can be reported.
            var oldPriority = dispatchCase.Priority;
            dispatchCase.OverridePriority(newPriority);
            _cases.Save(dispatchCase);
            AddAudit(AuditEventTypes.PriorityOverridden, performedBy, dispatchCase, null,
                oldPriority.ToString(), newPriority.ToString(), reason.Trim());
        }
    }

    private void AssignAvailableUnits(Case dispatchCase, string performedBy)
    {
        // Each required department can provide one closest available unit.
        var assigned = false;
        foreach (var responseType in dispatchCase.WaitingUnitTypes)
        {
            var units = _departments.Get(responseType)?.Units ?? [];
            var unit = _unitAssignment.SelectClosestAvailable(units, dispatchCase);
            if (unit is null) continue;
            // Store the distance that was used to choose this unit.
            var distance = _unitAssignment.CalculateDistanceKilometres(dispatchCase, unit);
            if (!dispatchCase.Assign(unit, distance)) continue;
            assigned = true;
            _departments.Save(unit);
            AddAudit(AuditEventTypes.UnitAssigned, performedBy, dispatchCase, unit.Identifier,
                "Available", "Assigned", $"Closest available unit selected at {distance:F2} km.");
            NotifySafely(unit, dispatchCase);
        }
        if (assigned) _cases.Save(dispatchCase);
    }

    private Case? AssignNextWaitingCase(Unit availableUnit, string performedBy)
    {
        // Recorded time creates a predictable first-in waiting queue.
        var waitingCase = _cases.GetAll()
            .Where(dispatchCase => dispatchCase.IsWaitingFor(availableUnit.Type))
            .OrderBy(dispatchCase => dispatchCase.RecordedAt)
            .ThenBy(dispatchCase => dispatchCase.Id)
            .FirstOrDefault();

        if (waitingCase is null) return null;
        var distance = _unitAssignment.CalculateDistanceKilometres(waitingCase, availableUnit);
        if (!waitingCase.Assign(availableUnit, distance)) return null;
        AddAudit(AuditEventTypes.UnitAssigned, performedBy, waitingCase, availableUnit.Identifier,
            "Available", "Assigned", $"Assigned to the oldest waiting case at {distance:F2} km.");
        NotifySafely(availableUnit, waitingCase);
        return waitingCase;
    }

    private void AddAudit(string eventType, string performedBy, Case? dispatchCase,
        string? unitIdentifier, string? oldValue, string? newValue, string? reason)
    {
        _audit.Add(new AuditEvent(
            Guid.NewGuid(), DateTimeOffset.UtcNow, eventType,
            string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy.Trim(),
            dispatchCase?.Id, dispatchCase?.CaseNumber, unitIdentifier, oldValue, newValue, reason));
    }

    private void NotifySafely(Unit unit, Case dispatchCase)
    {
        try
        {
            _notifier.Notify(unit, dispatchCase);
        }
        catch
        {
            // Appendix 2 classifies notification as non-critical. Assignment must remain successful
            // if the notification implementation is temporarily unavailable.
        }
    }
}

/// <summary>Contains the information required to create a new emergency case.</summary>
public sealed record CreateCaseRequest(string CallerName, string CallerPhone, string IncidentType,
    string Description, string Location, Severity Severity, IReadOnlyCollection<ResponseUnitType> RequiredUnitTypes,
    double Latitude = -36.8485, double Longitude = 174.7633);
