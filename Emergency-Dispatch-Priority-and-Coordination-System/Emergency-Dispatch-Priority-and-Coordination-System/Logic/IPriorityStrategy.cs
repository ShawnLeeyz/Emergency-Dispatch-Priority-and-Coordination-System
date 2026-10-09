using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>Allows the priority calculation rule to be replaced without changing dispatch.</summary>
public interface IPriorityStrategy { Priority Calculate(Case dispatchCase); }
