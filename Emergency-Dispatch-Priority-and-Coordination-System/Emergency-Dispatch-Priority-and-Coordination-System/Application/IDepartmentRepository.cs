using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Provides department data and saves changes made to response units.</summary>
public interface IDepartmentRepository
{
    IReadOnlyCollection<Department> GetAll();
    Department? Get(ResponseUnitType type);
    void Save(Unit unit);
}
