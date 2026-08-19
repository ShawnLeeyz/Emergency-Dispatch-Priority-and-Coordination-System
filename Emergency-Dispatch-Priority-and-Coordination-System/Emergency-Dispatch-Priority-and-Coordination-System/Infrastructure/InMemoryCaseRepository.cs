using System.Collections.Concurrent;
using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class InMemoryCaseRepository : ICaseRepository
{
    private readonly ConcurrentDictionary<Guid, Case> _cases = new();
    public void Add(Case dispatchCase) => _cases.TryAdd(dispatchCase.Id, dispatchCase);
    public Case? Get(Guid id) => _cases.GetValueOrDefault(id);
    public IReadOnlyCollection<Case> GetAll() => _cases.Values.OrderByDescending(c => c.RecordedAt).ToArray();
    public IReadOnlyCollection<Case> Search(string? callerName, string? caseId, DateOnly? date)
    {
        var hasCaller = !string.IsNullOrWhiteSpace(callerName);
        var hasCaseId = !string.IsNullOrWhiteSpace(caseId);
        var hasDate = date.HasValue;
        if (!hasCaller && !hasCaseId && !hasDate) return GetAll();

        return GetAll().Where(dispatchCase =>
            (hasCaller && dispatchCase.CallerName.Contains(callerName!, StringComparison.OrdinalIgnoreCase)) ||
            (hasCaseId && dispatchCase.CaseNumber.Contains(caseId!, StringComparison.OrdinalIgnoreCase)) ||
            (hasDate && DateOnly.FromDateTime(dispatchCase.RecordedAt.LocalDateTime) == date)).ToArray();
    }
}
