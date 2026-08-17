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
        _cases.Add(dispatchCase);
        AssignAvailableUnits(dispatchCase);
        return dispatchCase;
    }

    public void CloseCase(Guid caseId)
    {
        lock (_dispatchLock)
        {
            var dispatchCase = _cases.Get(caseId) ?? throw new KeyNotFoundException("The selected case no longer exists.");
            dispatchCase.Close();
        }
    }

    private void AssignAvailableUnits(Case dispatchCase)
    {
        lock (_dispatchLock)
        {
            foreach (var responseType in dispatchCase.RequiredUnitTypes)
            {
                var unit = _departments.Get(responseType)?.Units.FirstOrDefault(candidate => candidate.Availability == UnitAvailability.Available);
                if (unit is not null && dispatchCase.Assign(unit)) _notifier.Notify(unit, dispatchCase);
            }
        }
    }
}

public sealed record CreateCaseRequest(string CallerName, string CallerPhone, string IncidentType,
    string Description, string Location, Severity Severity, IReadOnlyCollection<ResponseUnitType> RequiredUnitTypes);
