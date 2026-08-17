using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>Small abstraction retained for callers that need a single assignment attempt.</summary>
public interface IUnitAssignmentService { bool TryAssign(Unit unit, Case dispatchCase); }
