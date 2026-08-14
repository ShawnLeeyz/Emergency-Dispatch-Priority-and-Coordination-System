using System;
using System.Collections.Generic;


/* This is the domain knowledge of all the attributes of the department, which includes
 * (Id, DepartmentType, name and units)
 * 
 */
namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain
{
    internal class Department
    {
        // This enum represents the type of department
        public enum DepartmentType { ambulance, fireFighter, police }

        public string departmentId { get; private set; } = Guid.NewGuid().ToString();
        public DepartmentType type { get; private set; }
        public string name { get; private set; }
        public List<Unit> units { get; private set; } = new();

        public Department(DepartmentType type, string name)
        {
            this.type = type;
            this.name = name;
        }
    }
}