using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using System;
using System.Collections.Generic;
using System.Text;

//Interface for the priority strategy
namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic
{
    internal interface IPriorityStrategy
    {
        Case.PriorityType Calculate(Case dispatchCase);
    }
}