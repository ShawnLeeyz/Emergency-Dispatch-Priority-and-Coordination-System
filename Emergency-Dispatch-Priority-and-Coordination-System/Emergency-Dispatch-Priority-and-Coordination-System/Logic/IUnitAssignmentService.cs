using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

public interface IUnitAssignmentService
{
    Unit? SelectClosestAvailable(IEnumerable<Unit> units, Case dispatchCase);
    double CalculateDistanceKilometres(Case dispatchCase, Unit unit);
}
