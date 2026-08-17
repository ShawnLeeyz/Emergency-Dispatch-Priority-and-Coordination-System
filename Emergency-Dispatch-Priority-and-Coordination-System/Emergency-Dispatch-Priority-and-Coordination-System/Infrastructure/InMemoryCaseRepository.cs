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
    public IReadOnlyCollection<Case> Search(string? term, DateOnly? date) => GetAll().Where(c =>
        (string.IsNullOrWhiteSpace(term) || c.CaseNumber.Contains(term, StringComparison.OrdinalIgnoreCase) || c.CallerName.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
        (!date.HasValue || DateOnly.FromDateTime(c.RecordedAt.LocalDateTime) == date)).ToArray();
}
