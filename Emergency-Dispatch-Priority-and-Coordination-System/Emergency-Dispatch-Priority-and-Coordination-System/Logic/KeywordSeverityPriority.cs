using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>Prototype decision policy. It is deliberately isolated so a department-specific policy can replace it.</summary>
public sealed class KeywordSeverityPriority : IPriorityStrategy
{
    private static readonly string[] HighRiskTerms = ["unconscious", "fire", "weapon", "chest pain", "not breathing"];
    private static readonly string[] MediumRiskTerms = ["injury", "accident", "collision"];

    public Priority Calculate(Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(dispatchCase);
        var text = dispatchCase.Description.ToLowerInvariant();
        if (HighRiskTerms.Any(text.Contains)) return Priority.High;
        if (MediumRiskTerms.Any(text.Contains)) return Priority.Medium;
        return dispatchCase.Severity switch { Severity.High => Priority.High, Severity.Medium => Priority.Medium, _ => Priority.Low };
    }
}
