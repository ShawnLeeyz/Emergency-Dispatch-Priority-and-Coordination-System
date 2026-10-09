using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Defines the storage operations used for persistent audit events.</summary>
public interface IAuditRepository
{
    void Add(AuditEvent auditEvent);
    IReadOnlyCollection<AuditEvent> GetAll();
}
