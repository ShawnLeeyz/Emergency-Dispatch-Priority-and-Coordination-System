using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Sends and retrieves the simple assignment notifications used by the prototype.</summary>
public interface IDispatchNotifier
{
    void Notify(Unit unit, Case dispatchCase);
    IReadOnlyCollection<DispatchNotification> GetAll();
}

/// <summary>Stores the message sent when a response unit receives a case.</summary>
public sealed record DispatchNotification(DateTimeOffset CreatedAt, string UnitIdentifier, ResponseUnitType DepartmentType,
    string CaseNumber, string IncidentType, string Location, string Message);
