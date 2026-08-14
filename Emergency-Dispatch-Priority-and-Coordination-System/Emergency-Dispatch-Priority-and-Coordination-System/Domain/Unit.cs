using System;
using System.Collections.Generic;
using System.Text;

/* This is the domain knoweldege of all the attributes of the units, which includes
 * (Id, UnitType, status, location, personnel count and Assigned Case)
 * 
 */
namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain
{
    public class Unit
    {
        //This enum relates to all status avaliable for the units
        public enum UnitStatus { Assigned, Unassigned }

        // This enum relates to all types of units that are available
        public enum UnitType { ambulance, fireFighter, police }

        //These are the attributes that are related towards the unit
        public string unitId { get; private set; } = Guid.NewGuid().ToString();
        public UnitType type { get; private set; }
        public UnitStatus status { get; set; } = UnitStatus.Unassigned;
        public string location { get; private set; }
        public int personnelCount { get; private set; }
        public Case? assignedCase { get; set; } = null;

        public Unit(UnitType type, string location, int personnelCount)
        {
            this.type = type;
            this.location = location;
            this.personnelCount = personnelCount;
        }
    }
}
