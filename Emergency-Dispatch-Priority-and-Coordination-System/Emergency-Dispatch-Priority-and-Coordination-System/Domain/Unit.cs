namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain;

public enum UnitAvailability { Available, Unavailable }

public sealed class Unit
{
    public Guid Id { get; }
    public string Identifier { get; }
    public ResponseUnitType Type { get; }
    public string Location { get; private set; }
    public int PersonnelCount { get; private set; }
    public UnitAvailability Availability { get; private set; } = UnitAvailability.Available;
    public Guid? AssignedCaseId { get; private set; }

    public Unit(string identifier, ResponseUnitType type, string location, int personnelCount)
        : this(Guid.NewGuid(), identifier, type, location, personnelCount,
            UnitAvailability.Available, null)
    {
    }

    internal Unit(Guid id, string identifier, ResponseUnitType type, string location, int personnelCount,
        UnitAvailability availability, Guid? assignedCaseId)
    {
        Id = id;
        Identifier = string.IsNullOrWhiteSpace(identifier) ? throw new ArgumentException("Identifier is required.", nameof(identifier)) : identifier.Trim();
        Type = type;
        Location = string.IsNullOrWhiteSpace(location) ? throw new ArgumentException("Location is required.", nameof(location)) : location.Trim();
        PersonnelCount = personnelCount > 0 ? personnelCount : throw new ArgumentOutOfRangeException(nameof(personnelCount));
        Availability = availability;
        AssignedCaseId = assignedCaseId;
    }

    public void UpdateDetails(string location, int personnelCount)
    {
        Location = string.IsNullOrWhiteSpace(location) ? throw new ArgumentException("Location is required.", nameof(location)) : location.Trim();
        PersonnelCount = personnelCount > 0 ? personnelCount : throw new ArgumentOutOfRangeException(nameof(personnelCount));
    }

    internal bool TryAssign(Case dispatchCase)
    {
        if (Availability != UnitAvailability.Available) return false;
        Availability = UnitAvailability.Unavailable;
        AssignedCaseId = dispatchCase.Id;
        return true;
    }

    internal void Release()
    {
        Availability = UnitAvailability.Available;
        AssignedCaseId = null;
    }
}
