using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Application;

/// <summary>Defines how cases are stored, retrieved, and searched.</summary>
public interface ICaseRepository
{
    void Add(Case dispatchCase);
    void Save(Case dispatchCase);
    Case? Get(Guid id);
    IReadOnlyCollection<Case> GetAll();
    IReadOnlyCollection<Case> Search(string? callerName, string? caseId, DateOnly? date);
}
