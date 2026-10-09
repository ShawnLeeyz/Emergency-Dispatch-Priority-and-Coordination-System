using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

/// <summary>Creates and stores response-unit assignment messages in SQLite.</summary>
public sealed class SqliteDispatchNotifier(SqliteDatabase database) : IDispatchNotifier
{
    public void Notify(Unit unit, Case dispatchCase)
    {
        var notification = new DispatchNotification(
            DateTimeOffset.UtcNow,
            unit.Identifier,
            unit.Type,
            dispatchCase.CaseNumber,
            dispatchCase.IncidentType,
            dispatchCase.Location,
            $"Respond to {dispatchCase.IncidentType} at {dispatchCase.Location}. Priority: {dispatchCase.Priority}.");

        database.AddNotification(notification);
    }

    public IReadOnlyCollection<DispatchNotification> GetAll() => database.GetNotifications();
}
