using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class SqliteAuditRepository(SqliteDatabase database) : IAuditRepository
{
    public void Add(AuditEvent auditEvent) => database.AddAuditEvent(auditEvent);
    public IReadOnlyCollection<AuditEvent> GetAll() => database.GetAuditEvents();
}
