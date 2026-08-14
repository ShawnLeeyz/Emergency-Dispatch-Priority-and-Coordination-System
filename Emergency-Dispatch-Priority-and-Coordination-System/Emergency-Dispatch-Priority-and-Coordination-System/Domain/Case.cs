using System;
using System.Collections.Generic;
using System.Text;
/* This is a domain file which contains the attributes of 
 * a case. It contains the severity, status, and priority of a case.
 */


namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain
{
    public class Case
    {

        // This enum represents the severity of a case
        public enum SeverityType { None, Low, Medium, High }
        
        // This enum represents the status of a case
        public enum StatusType { None, Open, InProgress, Closed }
        public enum PriorityType { Low, Medium, High }

        //All attributes relating towards the case are defined here
        public int callerId { get; private set; }
        public string caseId { get; private set; } = Guid.NewGuid().ToString();
        public string notice { get; private set; }
        public string description { get; private set; }
        public string location { get; private set; }
        public StatusType status { get; set; }
        public DateTime timeStamp { get; private set; } = DateTime.UtcNow;
        public SeverityType severityType { get; private set; }
        public PriorityType priorityType { get; set; }
        public Unit? assignedUnit { get; set; }

        public Case(int callerId, string notice, string description, string location, StatusType status, SeverityType severityType, PriorityType priorityType)
        {
            this.callerId = callerId;
            this.notice = notice;
            this.description = description;
            this.location = location;
            this.status = status;
            this.severityType = severityType;
            this.priorityType = priorityType;
        }
    }
}
