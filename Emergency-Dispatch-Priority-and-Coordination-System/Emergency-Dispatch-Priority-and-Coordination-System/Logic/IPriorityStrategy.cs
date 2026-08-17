using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

public interface IPriorityStrategy { Priority Calculate(Case dispatchCase); }
