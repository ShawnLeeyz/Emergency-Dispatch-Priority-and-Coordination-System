using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

/// <summary>Loads departments and saves response-unit changes in SQLite.</summary>
public sealed class SqliteDepartmentRepository(SqliteDatabase database) : IDepartmentRepository
{
    public IReadOnlyCollection<Department> GetAll() => database.GetDepartments();
    public Department? Get(ResponseUnitType type) => database.GetDepartment(type);
    public void Save(Unit unit) => database.SaveUnit(unit);
}
