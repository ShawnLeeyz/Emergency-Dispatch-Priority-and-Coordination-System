using System.Collections.Concurrent;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    public void Add(AuditEvent auditEvent) => _events.Enqueue(auditEvent);
    public IReadOnlyCollection<AuditEvent> GetAll() => _events.Reverse().ToArray();
}
