using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public sealed class DispatchService
{
    private readonly ICaseRepository _cases;
    private readonly IDepartmentRepository _departments;
    private readonly IPriorityStrategy _priorityStrategy;
    private readonly IDispatchNotifier _notifier;
    private readonly IAuditRepository _audit;
    private readonly Lock _dispatchLock = new();

    public DispatchService(ICaseRepository cases, IDepartmentRepository departments, IPriorityStrategy priorityStrategy,
        IDispatchNotifier notifier, IAuditRepository audit)
        => (_cases, _departments, _priorityStrategy, _notifier, _audit) =
            (cases, departments, priorityStrategy, notifier, audit);

    public Case CreateAndDispatch(CreateCaseRequest request, string performedBy = "System")
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispatchCase = new Case(request.CallerName, request.CallerPhone, request.IncidentType,
            request.Description, request.Location, request.Severity, request.RequiredUnitTypes);
        dispatchCase.SetPriority(_priorityStrategy.Calculate(dispatchCase));
        lock (_dispatchLock)
        {
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
            var previousStatus = dispatchCase.Status;
            dispatchCase.SignOff(unitId);
            AddAudit(AuditEventTypes.UnitSignedOff, performedBy, dispatchCase, unit.Identifier,
                "Assigned", "Available", "Response unit completed its assignment.");
            if (previousStatus != CaseStatus.Closed && dispatchCase.Status == CaseStatus.Closed)
                AddAudit(AuditEventTypes.CaseClosed, performedBy, dispatchCase, null,
                    previousStatus.ToString(), CaseStatus.Closed.ToString(), "All required units signed off.");

            var nextCase = AssignNextWaitingCase(unit, performedBy);
            _cases.Save(dispatchCase);
            if (nextCase is not null) _cases.Save(nextCase);
            _departments.Save(unit);
        }
    }

    public void UpdateUnit(ResponseUnitType departmentType, Guid unitId, string location, int personnelCount,
        string performedBy = "System")
    {
        lock (_dispatchLock)
        {
            var unit = _departments.Get(departmentType)?.Units.SingleOrDefault(candidate => candidate.Id == unitId)
                ?? throw new KeyNotFoundException("The selected response unit could not be found in that department.");
            var oldValue = $"Location: {unit.Location}; Personnel: {unit.PersonnelCount}";
            unit.UpdateDetails(location, personnelCount);
            _departments.Save(unit);
            AddAudit(AuditEventTypes.UnitUpdated, performedBy, null, unit.Identifier, oldValue,
                $"Location: {unit.Location}; Personnel: {unit.PersonnelCount}", "Unit details updated.");
        }
    }

    private void AssignAvailableUnits(Case dispatchCase, string performedBy)
    {
        var assigned = false;
        foreach (var responseType in dispatchCase.WaitingUnitTypes)
        {
            var unit = _departments.Get(responseType)?.Units.FirstOrDefault(candidate => candidate.Availability == UnitAvailability.Available);
            if (unit is null || !dispatchCase.Assign(unit)) continue;
            assigned = true;
            _departments.Save(unit);
            AddAudit(AuditEventTypes.UnitAssigned, performedBy, dispatchCase, unit.Identifier,
                "Available", "Assigned", "Automatically selected for the new case.");
            NotifySafely(unit, dispatchCase);
        }
        if (assigned) _cases.Save(dispatchCase);
    }

    private Case? AssignNextWaitingCase(Unit availableUnit, string performedBy)
    {
        var waitingCase = _cases.GetAll()
            .Where(dispatchCase => dispatchCase.IsWaitingFor(availableUnit.Type))
            .OrderBy(dispatchCase => dispatchCase.RecordedAt)
            .ThenBy(dispatchCase => dispatchCase.Id)
            .FirstOrDefault();

        if (waitingCase is null || !waitingCase.Assign(availableUnit)) return null;
        AddAudit(AuditEventTypes.UnitAssigned, performedBy, waitingCase, availableUnit.Identifier,
            "Available", "Assigned", "Automatically assigned to the oldest waiting case.");
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

public sealed record CreateCaseRequest(string CallerName, string CallerPhone, string IncidentType,
    string Description, string Location, Severity Severity, IReadOnlyCollection<ResponseUnitType> RequiredUnitTypes);
