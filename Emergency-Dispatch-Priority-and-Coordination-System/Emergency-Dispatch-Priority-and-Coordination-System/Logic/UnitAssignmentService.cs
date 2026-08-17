using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

public sealed class UnitAssignmentService : IUnitAssignmentService
{
    public bool TryAssign(Unit unit, Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(dispatchCase);
        return dispatchCase.Assign(unit);
    }
}
