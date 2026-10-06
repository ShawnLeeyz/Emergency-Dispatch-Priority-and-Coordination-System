using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using Emergency_Dispatch_Priority_and_Coordination_System.Logic;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public sealed class DispatchService
{
    private readonly ICaseRepository _cases;
    private readonly IDepartmentRepository _departments;
    private readonly IPriorityStrategy _priorityStrategy;
    private readonly IDispatchNotifier _notifier;
    private readonly Lock _dispatchLock = new();

    public DispatchService(ICaseRepository cases, IDepartmentRepository departments, IPriorityStrategy priorityStrategy, IDispatchNotifier notifier)
        => (_cases, _departments, _priorityStrategy, _notifier) = (cases, departments, priorityStrategy, notifier);

    public Case CreateAndDispatch(CreateCaseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var dispatchCase = new Case(request.CallerName, request.CallerPhone, request.IncidentType,
            request.Description, request.Location, request.Severity, request.RequiredUnitTypes);
        dispatchCase.SetPriority(_priorityStrategy.Calculate(dispatchCase));
        lock (_dispatchLock)
        {
            _cases.Add(dispatchCase);
            AssignAvailableUnits(dispatchCase);
        }
        return dispatchCase;
    }

    public void SignOffUnit(Guid caseId, Guid unitId, ResponseUnitType departmentType)
    {
        lock (_dispatchLock)
        {
            var dispatchCase = _cases.Get(caseId) ?? throw new KeyNotFoundException("The selected case no longer exists.");
            var unit = dispatchCase.AssignedUnits.SingleOrDefault(candidate => candidate.Id == unitId)
                ?? throw new InvalidOperationException("That unit is not actively assigned to this case.");
            if (unit.Type != departmentType)
                throw new InvalidOperationException("That unit is managed by a different department.");
            dispatchCase.SignOff(unitId);
            var nextCase = AssignNextWaitingCase(unit);
            _cases.Save(dispatchCase);
            if (nextCase is not null) _cases.Save(nextCase);
            _departments.Save(unit);
        }
    }

    public void UpdateUnit(ResponseUnitType departmentType, Guid unitId, string location, int personnelCount)
    {
        lock (_dispatchLock)
        {
            var unit = _departments.Get(departmentType)?.Units.SingleOrDefault(candidate => candidate.Id == unitId)
                ?? throw new KeyNotFoundException("The selected response unit could not be found in that department.");
            unit.UpdateDetails(location, personnelCount);
            _departments.Save(unit);
        }
    }

    private void AssignAvailableUnits(Case dispatchCase)
    {
        var assigned = false;
        foreach (var responseType in dispatchCase.WaitingUnitTypes)
        {
            var unit = _departments.Get(responseType)?.Units.FirstOrDefault(candidate => candidate.Availability == UnitAvailability.Available);
            if (unit is null || !dispatchCase.Assign(unit)) continue;
            assigned = true;
            _departments.Save(unit);
            NotifySafely(unit, dispatchCase);
        }
        if (assigned) _cases.Save(dispatchCase);
    }

    private Case? AssignNextWaitingCase(Unit availableUnit)
    {
        var waitingCase = _cases.GetAll()
            .Where(dispatchCase => dispatchCase.IsWaitingFor(availableUnit.Type))
            .OrderBy(dispatchCase => dispatchCase.RecordedAt)
            .ThenBy(dispatchCase => dispatchCase.Id)
            .FirstOrDefault();

        if (waitingCase is null || !waitingCase.Assign(availableUnit)) return null;
        NotifySafely(availableUnit, waitingCase);
        return waitingCase;
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
