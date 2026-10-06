using Emergency_Dispatch_Priority_and_Coordination_System.Application;
using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Infrastructure;

public sealed class SqliteCaseRepository(SqliteDatabase database) : ICaseRepository
{
    public void Add(Case dispatchCase) => database.AddCase(dispatchCase);
    public void Save(Case dispatchCase) => database.SaveCase(dispatchCase);
    public Case? Get(Guid id) => database.GetCase(id);
    public IReadOnlyCollection<Case> GetAll() => database.GetCases();

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
