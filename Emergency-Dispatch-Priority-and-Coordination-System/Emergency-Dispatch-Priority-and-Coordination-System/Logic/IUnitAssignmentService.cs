using System;
using System.Collections.Generic;
using System.Text;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

//Interface for the unit assignment service
namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic
{
    internal interface IUnitAssignmentService
    {
        bool TryAssign(Unit unit, Case dispatchCase);
    }
}
