using Emergency_Dispatch_Priority_and_Coordination_System.Domain;
using System;
using System.Collections.Generic;
using System.Text;

//This class contains all the logic on the automation on the PriorityStrategy
namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic
{
    internal class KeywordSeverityPriority : IPriorityStrategy
    {

        //This method checks the descriptors for a certain key word to determine the priority level
        public Case.PriorityType Calculate(Case dispatchCase)
        {
            string text = dispatchCase.description.ToLowerInvariant(); //convert to lower case for case-insensitive comparison

            if (text.Contains("unconscious") || text.Contains("fire") || text.Contains("weapon")) //Certain key words that trigger "High" priority
            {
                return Case.PriorityType.High;
            }

            if (text.Contains("injury") || text.Contains("accident")) //Certain key words that trigger "Medium" priority
            {
                return Case.PriorityType.Medium;
            }

            return Case.PriorityType.Low;
        }
    }
}
