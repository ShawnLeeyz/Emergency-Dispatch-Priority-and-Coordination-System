using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class InMemoryDepartmentRepository : IDepartmentRepository
{
    private readonly IReadOnlyCollection<Department> _departments =
    [
        new(ResponseUnitType.Medical, "Medical", [new("MED-01", ResponseUnitType.Medical, "Central Hospital", 2), new("MED-02", ResponseUnitType.Medical, "North Clinic", 2)]),
        new(ResponseUnitType.Police, "Police", [new("POL-01", ResponseUnitType.Police, "Central Station", 2), new("POL-02", ResponseUnitType.Police, "West Station", 2)]),
        new(ResponseUnitType.Fire, "Fire", [new("FIR-01", ResponseUnitType.Fire, "Fire Station 1", 4), new("FIR-02", ResponseUnitType.Fire, "Fire Station 2", 4)])
    ];

    public IReadOnlyCollection<Department> GetAll() => _departments;
    public Department? Get(ResponseUnitType type) => _departments.SingleOrDefault(d => d.Type == type);
}
