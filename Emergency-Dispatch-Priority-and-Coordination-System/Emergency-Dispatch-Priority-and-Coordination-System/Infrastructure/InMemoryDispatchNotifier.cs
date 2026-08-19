using System.Collections.Concurrent;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class InMemoryDispatchNotifier : IDispatchNotifier
{
    private readonly ConcurrentQueue<DispatchNotification> _notifications = new();
    public void Notify(Unit unit, Case dispatchCase) => _notifications.Enqueue(new(DateTimeOffset.UtcNow, unit.Identifier, dispatchCase.CaseNumber, $"Assigned to {dispatchCase.IncidentType} at {dispatchCase.Location}."));
    public IReadOnlyCollection<DispatchNotification> GetAll() => _notifications.Reverse().ToArray();
}
