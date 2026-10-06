using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

public interface IAuditRepository
{
    void Add(AuditEvent auditEvent);
    IReadOnlyCollection<AuditEvent> GetAll();
}
