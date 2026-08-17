using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public interface IDepartmentRepository
{
    IReadOnlyCollection<Department> GetAll();
    Department? Get(ResponseUnitType type);
}
