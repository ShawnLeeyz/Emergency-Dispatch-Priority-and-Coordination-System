using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public interface IDispatchNotifier
{
    void Notify(Unit unit, Case dispatchCase);
    IReadOnlyCollection<DispatchNotification> GetAll();
}

public sealed record DispatchNotification(DateTimeOffset CreatedAt, string UnitIdentifier, string CaseNumber, string Message);
