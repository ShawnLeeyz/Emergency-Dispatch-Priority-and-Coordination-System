using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>Defines how an available unit is selected and its distance is measured.</summary>
public interface IUnitAssignmentService
{
    Unit? SelectClosestAvailable(IEnumerable<Unit> units, Case dispatchCase);
    double CalculateDistanceKilometres(Case dispatchCase, Unit unit);
}
