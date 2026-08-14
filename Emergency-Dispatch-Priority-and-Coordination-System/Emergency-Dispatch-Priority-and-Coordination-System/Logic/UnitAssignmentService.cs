using System;
using System.Collections.Generic;
using System.Text;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

//This class is responsible for all the logic related to the unit domain class.
namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic
{
    internal class UnitAssignmentService : IUnitAssignmentService
    {
        //This method checks if the unit is currently assigned or not assigned. returns false or true based on the result
        public bool TryAssign(Unit unit, Case dispatchCase)
        {
            if (unit.status != Unit.UnitStatus.Unassigned) //Check that status is unassigned return false
            {
                return false;
            }

            unit.status = Unit.UnitStatus.Assigned;
            unit.assignedCase = dispatchCase;
            dispatchCase.status = Case.StatusType.InProgress;

            return true;
        }
    }
}