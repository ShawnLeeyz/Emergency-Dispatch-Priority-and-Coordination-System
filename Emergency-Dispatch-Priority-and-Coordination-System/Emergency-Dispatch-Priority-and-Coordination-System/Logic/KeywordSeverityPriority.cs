using Emergency_Dispatch_Priority_and_Coordination_System.Domain;

namespace Emergency_Dispatch_Priority_and_Coordination_System.Logic;

/// <summary>
/// Appendix 1 priority policy. Keywords are evaluated only for the departments requested by the case.
/// A matching High term takes precedence over Medium; dispatcher severity is the fallback when no term matches.
/// </summary>
public sealed class KeywordSeverityPriority : IPriorityStrategy
{
    private static readonly IReadOnlyDictionary<ResponseUnitType, string[]> HighRiskTerms =
        new Dictionary<ResponseUnitType, string[]>
        {
            [ResponseUnitType.Police] =
            [
                "gun", "firearm", "shooting", "weapon", "armed", "knife", "stabbing", "kill", "murder",
                "hostage", "kidnapping", "assault", "attacking", "suicide", "unconscious", "unresponsive",
                "bleeding"
            ],
            [ResponseUnitType.Medical] =
            [
                "unconscious", "unresponsive", "passed out", "not breathing", "choking", "heart attack",
                "chest pain", "cardiac", "stroke", "face drooping", "severe bleeding", "arterial"
            ],
            [ResponseUnitType.Fire] =
            [
                "trapped", "inside", "structure fire", "house fire", "building on fire", "explosion", "building gas leak",
                "chemical", "hazmat", "spill", "wildfire"
            ]
        };

    private static readonly IReadOnlyDictionary<ResponseUnitType, string[]> MediumRiskTerms =
        new Dictionary<ResponseUnitType, string[]>
        {
            [ResponseUnitType.Police] =
            [
                "fleeing", "running away", "suspect", "intruder", "fight", "brawl", "punching", "burglary",
                "breaking in", "robbery", "mugging", "domestic", "arguing", "screaming", "yelling",
                "trespassing", "vandalism", "smashing"
            ],
            [ResponseUnitType.Medical] =
            [
                "broken bone", "fracture", "conscious fall", "fall while conscious", "seizure has stopped",
                "stopped seizure", "moderate burn", "burn moderate", "severe pain", "dizzy", "fainting"
            ],
            [ResponseUnitType.Fire] =
            [
                "vehicle fire", "small wildfire", "grass fire", "scrub fire", "brush fire", "tree fire",
                "smell of smoke", "electrical sparks", "fire alarm", "smoke detector", "dumpster fire"
            ]
        };

    public Priority Calculate(Case dispatchCase)
    {
        ArgumentNullException.ThrowIfNull(dispatchCase);
        var text = $"{dispatchCase.IncidentType} {dispatchCase.Description}".ToLowerInvariant();
        var requiredTypes = dispatchCase.RequiredUnitTypes;

        if (requiredTypes.Any(type => ContainsAny(text, HighTermsExcludingOverriddenFireTerms(type))))
            return Priority.High;
        if (requiredTypes.Any(type => ContainsAny(text, MediumRiskTerms[type])))
            return Priority.Medium;
        if (requiredTypes.Contains(ResponseUnitType.Fire) && text.Contains("wildfire", StringComparison.Ordinal))
            return Priority.High;

        return dispatchCase.Severity switch { Severity.High => Priority.High, Severity.Medium => Priority.Medium, _ => Priority.Low };
    }

    private static IEnumerable<string> HighTermsExcludingOverriddenFireTerms(ResponseUnitType type) =>
        type == ResponseUnitType.Fire
            ? HighRiskTerms[type].Where(term => term != "wildfire")
            : HighRiskTerms[type];

    private static bool ContainsAny(string text, IEnumerable<string> terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));
}
